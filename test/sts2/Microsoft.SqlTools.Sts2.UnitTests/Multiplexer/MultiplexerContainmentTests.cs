//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    /// <summary>SPEC §6.5: STS2 death must not take legacy traffic down with it.</summary>
    public class MultiplexerContainmentTests : IDisposable
    {
        private readonly CancellationTokenSource testTimeoutSource = new(TimeSpan.FromSeconds(10));

        private CancellationToken TestTimeout => testTimeoutSource.Token;

        public void Dispose() => testTimeoutSource.Dispose();

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
        public async Task DiagnosticsExcludeCallerControlledContent()
        {
            const string reasonCanary = "secret-reason-select-star";
            const string methodCanary = "v2/secret-method-select-star";
            const string requestIdCanary = "secret-request-id-select-star";
            const string responseIdCanary = "secret-response-id-select-star";

            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead(reasonCanary);
            await h.StdoutFrameAsync(TestTimeout); // drain v2/fatal

            await h.ClientSendsAsync(
                $$"""{"jsonrpc":"2.0","method":"{{methodCanary}}","params":null}""",
                TestTimeout);
            await h.ClientSendsAsync(
                $$"""{"jsonrpc":"2.0","id":"{{requestIdCanary}}","method":"v2/query.execute","params":null}""",
                TestTimeout);
            await h.StdoutFrameAsync(TestTimeout); // drain synthesized unavailable response

            string unknownResponse = $$"""{"jsonrpc":"2.0","id":"{{responseIdCanary}}","result":null}""";
            await h.ClientSendsAsync(unknownResponse, TestTimeout);
            Assert.Equal(unknownResponse, await h.LegacyReceivesAsync(TestTimeout));

            Assert.All(h.Diagnostics, diagnostic =>
            {
                Assert.DoesNotContain(reasonCanary, diagnostic.Message, StringComparison.Ordinal);
                Assert.DoesNotContain(methodCanary, diagnostic.Message, StringComparison.Ordinal);
                Assert.DoesNotContain(requestIdCanary, diagnostic.Message, StringComparison.Ordinal);
                Assert.DoesNotContain(responseIdCanary, diagnostic.Message, StringComparison.Ordinal);
            });
        }

        [Fact]
        public async Task LegacyTrafficSurvivesSts2DeathBothDirections()
        {
            await using var h = new MuxHarness();
            h.Mux.MarkSts2Dead("dead");
            await h.StdoutFrameAsync(TestTimeout); // drain v2/fatal

            await Assert.ThrowsAnyAsync<InvalidOperationException>(
                () => h.Sts2SendsAsync("""{"jsonrpc":"2.0","method":"v2/after-death"}""", TestTimeout));

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":10,"method":"connection/connect"}""", TestTimeout);
            Assert.Contains("connection/connect", await h.LegacyReceivesAsync(TestTimeout));

            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":10,"result":{"connected":true}}""", TestTimeout);
            Assert.Contains("connected", await h.StdoutFrameAsync(TestTimeout));
        }

        [Theory]
        [InlineData("Content-Length: nope\r\n\r\n", "MalformedHeader")]
        [InlineData("Content-Length: 999\r\n\r\n", "OversizedFrame")]
        public async Task InvalidOutboundFrameFailsSts2WithoutBreakingLegacy(string header, string expectedStatus)
        {
            await using var h = new MuxHarness(new MultiplexerOptions { MaxFrameBytes = 256 });
            await h.Mux.Sts2Output.WriteAsync(Encoding.ASCII.GetBytes(header), TestTimeout);
            await h.Mux.Sts2Output.FlushAsync(TestTimeout);

            JsonElement fatal = JsonDocument.Parse(await h.StdoutFrameAsync(TestTimeout)).RootElement;
            Assert.Equal("v2/fatal", fatal.GetProperty("method").GetString());
            Assert.Contains(
                h.Diagnostics,
                diagnostic => diagnostic.Code == MultiplexerDiagnosticCodes.PumpFailure
                    && diagnostic.Message.Contains(expectedStatus, StringComparison.Ordinal));

            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":11,"result":"still alive"}""", TestTimeout);
            Assert.Contains("still alive", await h.StdoutFrameAsync(TestTimeout));
        }

        [Fact]
        public async Task UnmaterializableOutboundFrameFailsSts2Cleanly()
        {
            await using var h = new MuxHarness(new MultiplexerOptions { MaxFrameBytes = int.MaxValue });
            const string header = "Content-Length: 2147483647\r\n\r\n";

            await h.Mux.Sts2Output.WriteAsync(Encoding.ASCII.GetBytes(header), TestTimeout);
            await h.Mux.Sts2Output.FlushAsync(TestTimeout);

            JsonElement fatal = JsonDocument.Parse(await h.StdoutFrameAsync(TestTimeout)).RootElement;
            Assert.Equal("v2/fatal", fatal.GetProperty("method").GetString());
            Assert.Contains("FrameTooLarge", fatal.GetProperty("params").GetProperty("summary").GetString());
            Assert.DoesNotContain("2147483647", fatal.GetProperty("params").GetProperty("summary").GetString());

            await h.LegacySendsAsync("""{"jsonrpc":"2.0","id":12,"result":"still alive"}""", TestTimeout);
            Assert.Contains("still alive", await h.StdoutFrameAsync(TestTimeout));
        }
    }
}
