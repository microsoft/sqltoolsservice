//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>
    /// SPEC §16 M2: the connection vertical end-to-end on FakeDriver — coordinator,
    /// driver effect runner, secret lifecycle, stable error model, replay.
    /// </summary>
    public sealed class ConnectionFlowTests : IAsyncDisposable, IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-conn-test-" + Guid.NewGuid().ToString("N"));
        private readonly FakeDriver fakeDriver = new();
        private readonly SecretSideTable secrets = new();
        private readonly DriverEffectRunner effectRunner;
        private readonly Coordinator coordinator;
        private readonly ConcurrentQueue<OutboundRpcMessage> emitted = new();
        private int corrCounter;

        public ConnectionFlowTests()
        {
            effectRunner = new DriverEffectRunner(
                new Dictionary<string, IDbDriver> { ["fake"] = fakeDriver }, secrets);
            coordinator = new Coordinator(
                new JournalWriter("conn-test", new JournalOptions { Directory = directory }, new JournalRunInfo { ServiceVersion = "9.9.9" }),
                new CoordinatorOptions { RunId = "conn-test" },
                effectRunner,
                emitted.Enqueue);
            // Session config is a journaled root envelope (replay starts from the same state).
            // Synchronous completion: the channel is empty at construction.
            coordinator.PostControlAsync("session.start", JsonDocument.Parse("""
                {"serviceVersion":"9.9.9","drivers":[{"name":"fake","dialects":["neutral","tsql"],"production":false}]}
                """).RootElement).AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync() => await coordinator.DisposeAsync();

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private async Task<OutboundRpcMessage> RequestAsync(string method, string payloadJson)
        {
            string corr = "r-" + (++corrCounter);
            JsonElement? payload = payloadJson is null ? null : JsonNodeRedact(payloadJson);
            await coordinator.PostRpcRequestAsync(method, corr, payload);
            return await WaitForCorrAsync(corr);
        }

        /// <summary>Applies the same secret redaction the gateway performs (SPEC §8.5).</summary>
        private JsonElement JsonNodeRedact(string json)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(json);
            string redacted = SecretRedactor.Redact(node, secrets)!.ToJsonString();
            return JsonDocument.Parse(redacted).RootElement;
        }

        private async Task<OutboundRpcMessage> WaitForCorrAsync(string corr)
        {
            for (int spins = 0; spins < 1000; spins++)
            {
                if (emitted.FirstOrDefault(m => m.Corr == corr) is { } match)
                {
                    return match;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException("No response for corr " + corr);
        }

        private static string OpenPayload(string openId) => """
            {"openId":"OPENID","profile":{"server":"fake://local","database":"main","driver":"fake","auth":{"kind":"sqlLogin","user":"sa","password":"PASSWORD"},"options":{"connectTimeoutMs":5000}}}
            """.Replace("OPENID", openId).Replace("PASSWORD", SecretCanaries.Password);

        private static string OpenPayloadWithAuth(string openId, string authJson, string driver = "fake")
        {
            return JsonSerializer.Serialize(new
            {
                openId,
                profile = new
                {
                    server = "fake://local",
                    database = "main",
                    driver,
                    auth = JsonSerializer.Deserialize<JsonElement>(authJson),
                    options = new { connectTimeoutMs = 5000 },
                },
            });
        }

        [Fact]
        public async Task OpenSucceedsEndToEndAndScrubsSecrets()
        {
            OutboundRpcMessage open = await RequestAsync("v2/connection.open", OpenPayload("open-1"));

            Assert.Equal("rpc.out.result", open.Kind);
            string connectionId = open.Body!.Value.GetProperty("connectionId").GetString()!;
            Assert.StartsWith("c-", connectionId);
            Assert.Equal("Fake 1.0", open.Body!.Value.GetProperty("serverInfo").GetProperty("product").GetString());

            // SPEC §8.5: the secret was consumed and removed when the open completed.
            Assert.Equal(0, secrets.Count);
            Assert.Equal(1, fakeDriver.OpenSessionCount);

            // And the journal never saw the canary (I6).
            await coordinator.DisposeAsync();
            Assert.Empty(SecretCanaries.ScanDirectory(directory));
        }

        [Fact]
        public async Task AuthFailureMapsToStableErrorShape()
        {
            fakeDriver.EnqueueOpen(new FakeOpenBehavior { Outcome = "authFail" });
            OutboundRpcMessage error = await RequestAsync("v2/connection.open", OpenPayload("open-2"));

            Assert.Equal("rpc.out.error", error.Kind);
            JsonElement body = error.Body!.Value;
            Assert.Equal(-32040, body.GetProperty("code").GetInt32());
            Assert.Equal("Sts2.ConnectionFailed.Auth", body.GetProperty("data").GetProperty("code").GetString());
            Assert.Equal(0, secrets.Count); // secret scrubbed on failure too
            Assert.Equal(0, fakeDriver.OpenSessionCount);
        }

        [Theory]
        [InlineData("token")]
        [InlineData("accessToken")]
        public async Task AccessTokenCanonicalAndLegacyAliasResolveAtDriverEdge(string field)
        {
            string auth = $$"""{"kind":"accessToken","{{field}}":{{JsonSerializer.Serialize(SecretCanaries.AccessToken)}}}""";
            OutboundRpcMessage open = await RequestAsync(
                "v2/connection.open",
                OpenPayloadWithAuth("open-token-" + field, auth));

            Assert.Equal("rpc.out.result", open.Kind);
            Assert.NotNull(fakeDriver.LastOpenRequest);
            Assert.Equal("accessToken", fakeDriver.LastOpenRequest!.Auth.Kind);
            Assert.Equal(SecretCanaries.AccessToken, fakeDriver.LastOpenRequest.Auth.Secret);
            Assert.Equal(0, secrets.Count);
        }

        [Fact]
        public async Task UnknownDriverStillScrubsAuthenticationSecrets()
        {
            OutboundRpcMessage error = await RequestAsync(
                "v2/connection.open",
                OpenPayloadWithAuth(
                    "open-unknown-driver",
                    $$"""{"kind":"accessToken","token":{{JsonSerializer.Serialize(SecretCanaries.AccessToken)}}}""",
                    "missing"));

            Assert.Equal("rpc.out.error", error.Kind);
            Assert.Equal(
                Sts2ErrorCodes.Unavailable,
                error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
            Assert.Null(fakeDriver.LastOpenRequest);
            Assert.Equal(0, secrets.Count);
        }

        [Theory]
        [InlineData("""{"kind":"accessToken","password":"pw","token":"jwt"}""")]
        [InlineData("""{"kind":"accessToken","password":123,"token":"jwt"}""")]
        [InlineData("""{"kind":"accessToken","token":"jwt-1","accessToken":"jwt-2"}""")]
        [InlineData("""{"kind":"accessToken","token":"jwt","metadata":"extra"}""")]
        [InlineData("""{"kind":"sqlLogin","password":"","token":"jwt"}""")]
        [InlineData("""{"kind":"integrated","accessToken":"jwt"}""")]
        public async Task MixedAuthenticationCredentialsReturnInvalidRequest(string auth)
        {
            OutboundRpcMessage error = await RequestAsync(
                "v2/connection.open",
                OpenPayloadWithAuth("open-mixed", auth));

            Assert.Equal("rpc.out.error", error.Kind);
            Assert.Equal(
                Sts2ErrorCodes.InvalidRequest,
                error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
            Assert.Null(fakeDriver.LastOpenRequest);
            Assert.Equal(0, secrets.Count);
        }

        [Theory]
        [InlineData("""{"kind":"accessToken"}""")]
        [InlineData("""{"kind":"accessToken","token":""}""")]
        [InlineData("""{"kind":"accessToken","accessToken":""}""")]
        [InlineData("""{"kind":"accessToken","metadata":{"refresh":"nested-secret"}}""")]
        public async Task MissingOrEmptyAccessTokenReturnsInvalidRequest(string auth)
        {
            OutboundRpcMessage error = await RequestAsync(
                "v2/connection.open",
                OpenPayloadWithAuth("open-empty-token", auth));

            Assert.Equal("rpc.out.error", error.Kind);
            Assert.Equal(
                Sts2ErrorCodes.InvalidRequest,
                error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
            Assert.Null(fakeDriver.LastOpenRequest);
            Assert.Equal(0, secrets.Count);
        }

        [Fact]
        public async Task EmptySqlPasswordRemainsValid()
        {
            OutboundRpcMessage open = await RequestAsync(
                "v2/connection.open",
                OpenPayloadWithAuth(
                    "open-empty-password",
                    """{"kind":"sqlLogin","user":"sa","password":""}"""));

            Assert.Equal("rpc.out.result", open.Kind);
            Assert.NotNull(fakeDriver.LastOpenRequest);
            Assert.Equal(string.Empty, fakeDriver.LastOpenRequest!.Auth.Secret);
            Assert.Equal(0, secrets.Count);
        }

        [Fact]
        public async Task HangingOpenIsCanceledByConnectionCancel()
        {
            fakeDriver.EnqueueOpen(new FakeOpenBehavior { Outcome = "hang" });

            string openCorr = "r-" + (++corrCounter);
            await coordinator.PostRpcRequestAsync("v2/connection.open", openCorr, JsonNodeRedact(OpenPayload("open-3")));

            OutboundRpcMessage cancel = await RequestAsync("v2/connection.cancel", """{"openId":"open-3"}""");
            Assert.Equal("rpc.out.result", cancel.Kind);

            OutboundRpcMessage open = await WaitForCorrAsync(openCorr);
            Assert.Equal("rpc.out.error", open.Kind);
            Assert.Equal("Sts2.Canceled", open.Body!.Value.GetProperty("data").GetProperty("code").GetString());
            Assert.Equal(0, fakeDriver.OpenSessionCount);
            Assert.Equal(0, secrets.Count);
        }

        [Fact]
        public async Task CancelIsIdempotentForUnknownAndCompletedOpens()
        {
            OutboundRpcMessage unknown = await RequestAsync("v2/connection.cancel", """{"openId":"never-existed"}""");
            Assert.Equal("rpc.out.result", unknown.Kind);

            await RequestAsync("v2/connection.open", OpenPayload("open-4"));
            OutboundRpcMessage afterComplete = await RequestAsync("v2/connection.cancel", """{"openId":"open-4"}""");
            Assert.Equal("rpc.out.result", afterComplete.Kind); // completed open: still {}
        }

        [Fact]
        public async Task CloseIsIdempotentAndReleasesLeases()
        {
            OutboundRpcMessage open = await RequestAsync("v2/connection.open", OpenPayload("open-5"));
            string connectionId = open.Body!.Value.GetProperty("connectionId").GetString()!;
            Assert.Equal(1, fakeDriver.OpenSessionCount);

            OutboundRpcMessage close = await RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            Assert.Equal("rpc.out.result", close.Kind);
            Assert.Equal(0, fakeDriver.OpenSessionCount); // I8

            OutboundRpcMessage closeAgain = await RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            Assert.Equal("rpc.out.result", closeAgain.Kind);
            OutboundRpcMessage closeUnknown = await RequestAsync("v2/connection.close", """{"connectionId":"c-nope"}""");
            Assert.Equal("rpc.out.result", closeUnknown.Kind);
        }

        [Fact]
        public async Task DuplicateOpenIdIsRejected()
        {
            fakeDriver.EnqueueOpen(new FakeOpenBehavior { Outcome = "hang" });
            string openCorr = "r-" + (++corrCounter);
            await coordinator.PostRpcRequestAsync("v2/connection.open", openCorr, JsonNodeRedact(OpenPayload("open-6")));

            OutboundRpcMessage duplicate = await RequestAsync("v2/connection.open", OpenPayload("open-6"));
            Assert.Equal("rpc.out.error", duplicate.Kind);
            Assert.Equal("Sts2.InvalidRequest", duplicate.Body!.Value.GetProperty("data").GetProperty("code").GetString());

            await RequestAsync("v2/connection.cancel", """{"openId":"open-6"}"""); // unblock the hang
            await WaitForCorrAsync(openCorr);
        }

        [Fact]
        public async Task InitializeIsIdempotentAndRejectsMustUnderstand()
        {
            OutboundRpcMessage first = await RequestAsync("v2/initialize", """{"clientName":"test","requestedSpecVersion":"2.0"}""");
            Assert.Equal("rpc.out.result", first.Kind);
            Assert.Equal("2.0.0-preview.1", first.Body!.Value.GetProperty("specVersion").GetString());
            Assert.Equal("fake", first.Body!.Value.GetProperty("drivers")[0].GetProperty("name").GetString());
            Assert.Equal(1000, first.Body!.Value.GetProperty("limits").GetProperty("pageRows").GetInt32());

            OutboundRpcMessage second = await RequestAsync("v2/initialize", """{"clientName":"test"}""");
            Assert.Equal("rpc.out.result", second.Kind); // idempotent, no reset

            OutboundRpcMessage rejected = await RequestAsync("v2/initialize", """{"mustUnderstand_newThing":true}""");
            Assert.Equal("rpc.out.error", rejected.Kind);
            Assert.Equal("Sts2.InvalidRequest", rejected.Body!.Value.GetProperty("data").GetProperty("code").GetString());
        }

        [Fact]
        public async Task PingReportsRealJournalSeq()
        {
            await RequestAsync("v2/diagnostics.ping", """{"echo":"warm"}""");
            OutboundRpcMessage ping = await RequestAsync("v2/diagnostics.ping", """{"echo":"x"}""");
            Assert.Equal("rpc.out.result", ping.Kind);
            Assert.True(ping.Body!.Value.GetProperty("latestJournalSeq").GetInt64() >= 3);
            Assert.Equal("ok", ping.Body!.Value.GetProperty("health").GetString());
        }

        [Fact]
        public async Task ConnectionSessionReplaysIdentically()
        {
            fakeDriver.EnqueueOpen(new FakeOpenBehavior()); // ok
            fakeDriver.EnqueueOpen(new FakeOpenBehavior { Outcome = "authFail" });

            await RequestAsync("v2/initialize", """{"clientName":"replay"}""");
            OutboundRpcMessage open = await RequestAsync("v2/connection.open", OpenPayload("open-7"));
            string connectionId = open.Body!.Value.GetProperty("connectionId").GetString()!;
            await RequestAsync("v2/connection.open", OpenPayload("open-8")); // auth fail
            await RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            await coordinator.DisposeAsync();

            ReplayResult replay = JournalReplayer.Replay(JournalReader.ReadAll(directory));
            Assert.True(replay.Identical,
                "divergence: " + replay.Divergence?.Recorded + " vs " + replay.Divergence?.Replayed);
            Assert.Empty(replay.FinalState.Connections);
        }
    }
}
