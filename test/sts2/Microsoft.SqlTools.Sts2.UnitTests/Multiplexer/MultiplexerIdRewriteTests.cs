//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    /// <summary>
    /// SPEC §6.3 / I13: server-initiated request ids are rewritten to globally unique
    /// public ids; responses are restored to the exact original id representation.
    /// </summary>
    public class MultiplexerIdRewriteTests
    {
        private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        private static (string PublicId, JsonElement Root) ParseRequest(string frameJson)
        {
            JsonElement root = JsonDocument.Parse(frameJson).RootElement;
            Assert.Equal(JsonValueKind.String, root.GetProperty("id").ValueKind); // public ids are strings
            return (root.GetProperty("id").GetString()!, root);
        }

        [Fact]
        public async Task CollidingNumericServerRequestIdsAreRewrittenDistinct()
        {
            await using var h = new MuxHarness();
            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":1,"method":"workspace/configuration","params":{}}""", TestTimeout);
            await h.Sts2SendsAsync("""{"jsonrpc":"2.0","id":1,"method":"v2/client.capabilities","params":{}}""", TestTimeout);

            (string id1, JsonElement r1) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));
            (string id2, JsonElement r2) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            Assert.NotEqual(id1, id2);
            // Both original methods made it out, whatever the interleaving.
            string methods = r1.GetProperty("method").GetString() + "|" + r2.GetProperty("method").GetString();
            Assert.Contains("workspace/configuration", methods);
            Assert.Contains("v2/client.capabilities", methods);
        }

        [Fact]
        public async Task KnownNotificationMethodStillRewritesAnIdSeenBeforeMethod()
        {
            await using var h = new MuxHarness();
            await h.Sts2SendsAsync(
                """{"jsonrpc":"2.0","id":17,"method":"v2/query.rows","params":{"rows":[]}}""",
                TestTimeout);

            (string publicId, JsonElement request) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));
            Assert.StartsWith("sts2mux-", publicId, StringComparison.Ordinal);
            Assert.Equal("v2/query.rows", request.GetProperty("method").GetString());

            await h.ClientSendsAsync($$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":null}""", TestTimeout);
            JsonElement restored = JsonDocument.Parse(await h.Sts2ReceivesAsync(TestTimeout)).RootElement;
            Assert.Equal(17, restored.GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task ResponsesAreRestoredToExactOriginalIdAndChannel()
        {
            await using var h = new MuxHarness();
            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":1,"method":"workspace/configuration"}""", TestTimeout);
            await h.Sts2SendsAsync("""{"jsonrpc":"2.0","id":1,"method":"v2/client.capabilities"}""", TestTimeout);

            (string pub1, JsonElement r1) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));
            (string pub2, JsonElement r2) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));
            string legacyPublicId = r1.GetProperty("method").GetString()!.StartsWith("v2/", StringComparison.Ordinal) ? pub2 : pub1;
            string sts2PublicId = legacyPublicId == pub1 ? pub2 : pub1;

            // Client answers both, out of order.
            await h.ClientSendsAsync($"{{\"jsonrpc\":\"2.0\",\"id\":\"{sts2PublicId}\",\"result\":{{\"caps\":[]}}}}", TestTimeout);
            await h.ClientSendsAsync($"{{\"jsonrpc\":\"2.0\",\"id\":\"{legacyPublicId}\",\"result\":{{\"settings\":{{}}}}}}", TestTimeout);

            string sts2Response = await h.Sts2ReceivesAsync(TestTimeout);
            string legacyResponse = await h.LegacyReceivesAsync(TestTimeout);

            // Exact original representation: number 1, not string "1".
            Assert.Equal(JsonValueKind.Number, JsonDocument.Parse(sts2Response).RootElement.GetProperty("id").ValueKind);
            Assert.Equal(1, JsonDocument.Parse(sts2Response).RootElement.GetProperty("id").GetInt32());
            Assert.Equal(JsonValueKind.Number, JsonDocument.Parse(legacyResponse).RootElement.GetProperty("id").ValueKind);
            Assert.Equal(1, JsonDocument.Parse(legacyResponse).RootElement.GetProperty("id").GetInt32());
            Assert.Contains("settings", legacyResponse);
            Assert.Contains("caps", sts2Response);
        }

        [Fact]
        public async Task StringIdRepresentationIsPreserved()
        {
            await using var h = new MuxHarness();
            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":"req-abc","method":"client/registerCapability"}""", TestTimeout);
            (string publicId, _) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            await h.ClientSendsAsync($$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":null}""", TestTimeout);
            JsonElement restored = JsonDocument.Parse(await h.LegacyReceivesAsync(TestTimeout)).RootElement;
            Assert.Equal(JsonValueKind.String, restored.GetProperty("id").ValueKind);
            Assert.Equal("req-abc", restored.GetProperty("id").GetString());
        }

        [Fact]
        public async Task DuplicateResponseGoesToLegacyWithDiagnostic()
        {
            await using var h = new MuxHarness();
            await h.Sts2SendsAsync("""{"jsonrpc":"2.0","id":3,"method":"v2/server.req"}""", TestTimeout);
            (string publicId, _) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            await h.ClientSendsAsync($$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":1}""", TestTimeout);
            Assert.Contains("\"id\":3", await h.Sts2ReceivesAsync(TestTimeout));

            // Duplicate: entry was consumed; falls back to legacy + diagnostic.
            string dup = $$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":2}""";
            await h.ClientSendsAsync(dup, TestTimeout);
            Assert.Equal(dup, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }

        [Fact]
        public async Task IdTableEntriesExpireAfterTtl()
        {
            var clock = new ManualTimeProvider();
            await using var h = new MuxHarness(new MultiplexerOptions
            {
                TimeProvider = clock,
                OutboundRequestIdTtl = TimeSpan.FromMinutes(5),
            });

            await h.Sts2SendsAsync("""{"jsonrpc":"2.0","id":9,"method":"v2/server.req"}""", TestTimeout);
            (string publicId, _) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            clock.Advance(TimeSpan.FromMinutes(6));

            string late = $$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":"too late"}""";
            await h.ClientSendsAsync(late, TestTimeout);
            Assert.Equal(late, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }

        [Fact]
        public async Task Sts2EntriesAreDroppedWhenChannelDies()
        {
            await using var h = new MuxHarness();
            await h.Sts2SendsAsync("""{"jsonrpc":"2.0","id":4,"method":"v2/server.req"}""", TestTimeout);
            (string publicId, _) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            h.Mux.MarkSts2Dead("test-induced death");
            await h.StdoutFrameAsync(TestTimeout); // drain the v2/fatal notification

            string orphan = $$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":1}""";
            await h.ClientSendsAsync(orphan, TestTimeout);
            Assert.Equal(orphan, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }

        [Fact]
        public async Task IdTableIsClearedOnExit()
        {
            var sink = new TestLifecycleSink();
            await using var h = new MuxHarness(lifecycleSink: sink);
            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":6,"method":"window/showMessageRequest"}""", TestTimeout);
            (string publicId, _) = ParseRequest(await h.StdoutFrameAsync(TestTimeout));

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"exit"}""", TestTimeout);
            Assert.Contains("exit", await h.LegacyReceivesAsync(TestTimeout));

            string late = $$"""{"jsonrpc":"2.0","id":"{{publicId}}","result":1}""";
            await h.ClientSendsAsync(late, TestTimeout);
            Assert.Equal(late, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }
    }
}
