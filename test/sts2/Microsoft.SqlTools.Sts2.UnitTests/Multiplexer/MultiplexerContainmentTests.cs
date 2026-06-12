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
    /// <summary>SPEC §6.5: STS2 death must not take legacy traffic down with it.</summary>
    public class MultiplexerContainmentTests
    {
        private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        [Fact]
        public async Task MarkSts2DeadEmitsFatalNotificationOnce()
        {
            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead("poison message in coordinator", journalPath: "/logs/sts2/journal-run-1.jsonl");
            h.Mux.MarkSts2Dead("second call is a no-op");

            JsonElement fatal = JsonDocument.Parse(await h.StdoutFrameAsync(TestTimeout)).RootElement;
            Assert.Equal("v2/fatal", fatal.GetProperty("method").GetString());
            Assert.Contains("poison", fatal.GetProperty("params").GetProperty("summary").GetString());
            Assert.Equal("/logs/sts2/journal-run-1.jsonl", fatal.GetProperty("params").GetProperty("journalPath").GetString());

            // Second MarkSts2Dead must not emit a second v2/fatal: next stdout frame is legacy traffic.
            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":1,"result":"still alive"}""", TestTimeout);
            Assert.Contains("still alive", await h.StdoutFrameAsync(TestTimeout));
        }

        [Fact]
        public async Task V2RequestsAfterDeathGetSynthesizedUnavailableError()
        {
            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead("dead");
            await h.StdoutFrameAsync(TestTimeout); // drain v2/fatal

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":"r-77","method":"v2/query.execute","params":{}}""", TestTimeout);
            JsonElement error = JsonDocument.Parse(await h.StdoutFrameAsync(TestTimeout)).RootElement;

            Assert.Equal("r-77", error.GetProperty("id").GetString());
            Assert.True(error.TryGetProperty("error", out JsonElement err));
            Assert.Equal("Sts2.Unavailable", err.GetProperty("data").GetProperty("code").GetString());
            Assert.Equal(JsonValueKind.Number, err.GetProperty("code").ValueKind); // numeric JSON-RPC code (I12)
        }

        [Fact]
        public async Task V2NotificationsAfterDeathAreDroppedWithDiagnostic()
        {
            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead("dead");
            await h.StdoutFrameAsync(TestTimeout); // drain v2/fatal

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"v2/query.ack","params":{}}""", TestTimeout);
            // Marker proves the notification was not delivered anywhere and pumping continues.
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":1,"method":"legacy/marker"}""", TestTimeout);
            Assert.Contains("legacy/marker", await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.Sts2Dead);
        }

        [Fact]
        public async Task LegacyTrafficSurvivesSts2DeathBothDirections()
        {
            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead("dead");
            await h.StdoutFrameAsync(TestTimeout); // drain v2/fatal

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":10,"method":"connection/connect"}""", TestTimeout);
            Assert.Contains("connection/connect", await h.LegacyReceivesAsync(TestTimeout));

            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":10,"result":{"connected":true}}""", TestTimeout);
            Assert.Contains("connected", await h.StdoutFrameAsync(TestTimeout));
        }
    }
}
