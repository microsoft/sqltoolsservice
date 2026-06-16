//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Observability;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using Microsoft.SqlTools.Sts2.Testing;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>
    /// Test harness composing FakeDriver + secrets + DriverEffectRunner + Coordinator,
    /// mirroring the production Sts2Session wiring minus the RPC transport.
    /// </summary>
    internal sealed class Sts2TestSession : IAsyncDisposable
    {
        private int corrCounter;

        public Sts2TestSession(string directory, string runId = "test-session", string rowCapture = "full", string sqlCapture = "text",
            IReadOnlyList<IEnvelopeSink>? auxSinks = null)
        {
            Driver = new FakeDriver();
            Secrets = new SecretSideTable();
            EffectRunner = new DriverEffectRunner(new Dictionary<string, IDbDriver> { ["fake"] = Driver }, Secrets);
            Coordinator = new Coordinator(
                new JournalWriter(runId, new JournalOptions { Directory = directory }, new JournalRunInfo { ServiceVersion = "9.9.9" }),
                new CoordinatorOptions { RunId = runId, RowCapture = rowCapture, SqlCapture = sqlCapture },
                EffectRunner,
                Emitted.Enqueue,
                auxSinks);
            Coordinator.PostControlAsync("session.start", JsonDocument.Parse("""
                {"serviceVersion":"9.9.9","drivers":[{"name":"fake","dialects":["neutral","tsql"],"production":false}]}
                """).RootElement).AsTask().GetAwaiter().GetResult();
        }

        public FakeDriver Driver { get; }

        public SecretSideTable Secrets { get; }

        public DriverEffectRunner EffectRunner { get; }

        public Coordinator Coordinator { get; }

        public ConcurrentQueue<OutboundRpcMessage> Emitted { get; } = new();

        /// <summary>The standard canary-credentialed open payload for the fake driver.</summary>
        public static string OpenPayload(string openId) => """
            {"openId":"OPENID","profile":{"server":"fake://local","database":"main","driver":"fake","auth":{"kind":"sqlLogin","user":"sa","password":"PASSWORD"},"options":{"connectTimeoutMs":5000}}}
            """.Replace("OPENID", openId).Replace("PASSWORD", SecretCanaries.Password);

        /// <summary>Posts a request (gateway-equivalent redaction applied) and awaits its terminal.</summary>
        public async Task<OutboundRpcMessage> RequestAsync(string method, string? payloadJson)
        {
            string corr = NextCorr();
            await PostRequestAsync(method, corr, payloadJson);
            return await WaitForCorrAsync(corr);
        }

        /// <summary>Posts a request without awaiting; returns the corr for later <see cref="WaitForCorrAsync"/>.</summary>
        public async Task<string> FireRequestAsync(string method, string? payloadJson)
        {
            string corr = NextCorr();
            await PostRequestAsync(method, corr, payloadJson);
            return corr;
        }

        public ValueTask NotifyAsync(string method, string payloadJson) =>
            Coordinator.PostRpcNotificationAsync(method, JsonDocument.Parse(payloadJson).RootElement);

        public async Task<OutboundRpcMessage> WaitForCorrAsync(string corr)
        {
            for (int spins = 0; spins < 1500; spins++)
            {
                if (Emitted.FirstOrDefault(m => m.Corr == corr) is { } match)
                {
                    return match;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException("No terminal response for corr " + corr);
        }

        /// <summary>Waits until at least <paramref name="count"/> notifications of <paramref name="method"/> arrived.</summary>
        public async Task<List<OutboundRpcMessage>> WaitForNotificationsAsync(string method, int count)
        {
            for (int spins = 0; spins < 1500; spins++)
            {
                List<OutboundRpcMessage> matches = Emitted.Where(m => m.Kind == "rpc.out.notify" && m.Type == method).ToList();
                if (matches.Count >= count)
                {
                    return matches;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException($"Expected {count} {method} notifications; have " +
                Emitted.Count(m => m.Kind == "rpc.out.notify" && m.Type == method));
        }

        /// <summary>Opens a connection and returns its connectionId.</summary>
        public async Task<string> OpenConnectionAsync(string openId = "open-1")
        {
            OutboundRpcMessage open = await RequestAsync("v2/connection.open", OpenPayload(openId));
            if (open.Kind != "rpc.out.result")
            {
                throw new InvalidOperationException("open failed: " + open.Body?.GetRawText());
            }
            return open.Body!.Value.GetProperty("connectionId").GetString()!;
        }

        public async ValueTask DisposeAsync()
        {
            await Coordinator.DisposeAsync();
            await EffectRunner.DisposeLeakedSessionsAsync();
        }

        private string NextCorr() => "r-" + Interlocked.Increment(ref corrCounter).ToString(CultureInfo.InvariantCulture);

        private async Task PostRequestAsync(string method, string corr, string? payloadJson)
        {
            JsonElement? payload = null;
            if (payloadJson is not null)
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
                payload = JsonDocument.Parse(SecretRedactor.Redact(node, Secrets)!.ToJsonString()).RootElement;
            }
            await Coordinator.PostRpcRequestAsync(method, corr, payload);
        }
    }
}
