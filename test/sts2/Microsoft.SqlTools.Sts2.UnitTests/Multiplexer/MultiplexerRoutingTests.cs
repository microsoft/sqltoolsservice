//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    public class MultiplexerRoutingTests
    {
        private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        [Fact]
        public async Task V2MethodsRouteToSts2()
        {
            await using var h = new MuxHarness();
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":1,"method":"v2/diagnostics.ping","params":{}}""", TestTimeout);
            string received = await h.Sts2ReceivesAsync(TestTimeout);
            Assert.Contains("v2/diagnostics.ping", received);
        }

        [Fact]
        public async Task NonV2MethodsRouteToLegacy()
        {
            await using var h = new MuxHarness();
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":2,"method":"connection/connect","params":{}}""", TestTimeout);
            string received = await h.LegacyReceivesAsync(TestTimeout);
            Assert.Contains("connection/connect", received);
        }

        [Fact]
        public async Task RoutedFramesArriveByteIdentical()
        {
            await using var h = new MuxHarness();
            string json = """{"jsonrpc":"2.0","id":99,"method":"textDocument/didOpen","params":{"text":"select 1 é"}}""";
            await h.ClientSendsAsync(json, TestTimeout);
            string received = await h.LegacyReceivesAsync(TestTimeout);
            Assert.Equal(json, received);
        }

        [Fact]
        public async Task InterleavedTrafficKeepsOrderPerChannel()
        {
            await using var h = new MuxHarness();
            for (int i = 0; i < 20; i++)
            {
                string target = i % 2 == 0 ? "v2/q" : "legacy/q";
                await h.ClientSendsAsync($$"""{"jsonrpc":"2.0","id":{{i}},"method":"{{target}}{{i}}"}""", TestTimeout);
            }
            for (int i = 0; i < 20; i += 2)
            {
                Assert.Contains($"v2/q{i}", await h.Sts2ReceivesAsync(TestTimeout));
                Assert.Contains($"legacy/q{i + 1}", await h.LegacyReceivesAsync(TestTimeout));
            }
        }

        [Fact]
        public async Task PartialAndCoalescedChunksReassembleIntoFrames()
        {
            await using var h = new MuxHarness();
            byte[] f1 = Frames.Frame("""{"jsonrpc":"2.0","id":1,"method":"legacy/a"}""");
            byte[] f2 = Frames.Frame("""{"jsonrpc":"2.0","id":2,"method":"legacy/b"}""");
            byte[] all = f1.Concat(f2).ToArray();

            // Deliver in pathological chunk sizes: 1 byte, 3 bytes, then the rest coalesced.
            await h.ClientSendsRawAsync(all[..1], TestTimeout);
            await h.ClientSendsRawAsync(all[1..4], TestTimeout);
            await h.ClientSendsRawAsync(all[4..], TestTimeout);

            Assert.Contains("legacy/a", await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains("legacy/b", await h.LegacyReceivesAsync(TestTimeout));
        }

        [Theory]
        [InlineData("Content-Length:{0}\r\n\r\n")]                                                       // no space
        [InlineData("Content-Length: {0}\r\n\r\n")]                                                      // canonical
        [InlineData("content-length: {0}\r\n\r\n")]                                                      // lowercase
        [InlineData("Content-Length: {0}\r\nContent-Type: application/vscode-jsonrpc;charset=utf-8\r\n\r\n")] // extra header
        [InlineData("Content-Type: application/vscode-jsonrpc;charset=utf-8\r\nContent-Length: {0}\r\n\r\n")] // header order
        public async Task ContentLengthHeaderVariantsAreAccepted(string headerTemplate)
        {
            await using var h = new MuxHarness();
            string json = """{"jsonrpc":"2.0","id":7,"method":"legacy/x"}""";
            byte[] payload = Encoding.UTF8.GetBytes(json);
            string header = string.Format(System.Globalization.CultureInfo.InvariantCulture, headerTemplate, payload.Length);
            await h.ClientSendsRawAsync(Encoding.ASCII.GetBytes(header).Concat(payload).ToArray(), TestTimeout);
            Assert.Equal(json, await h.LegacyReceivesAsync(TestTimeout));
        }

        [Fact]
        public async Task MalformedJsonPayloadForwardsRawToLegacyWithDiagnostic()
        {
            await using var h = new MuxHarness();
            string notJson = "{this is not json at all";
            await h.ClientSendsAsync(notJson, TestTimeout);
            Assert.Equal(notJson, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.MalformedPayload);
        }

        [Fact]
        public async Task OversizedFrameForwardsToLegacyWithDiagnostic()
        {
            await using var h = new MuxHarness(new MultiplexerOptions { MaxFrameBytes = 256 });
            string big = """{"jsonrpc":"2.0","id":1,"method":"legacy/big","params":{"pad":" """ + new string('x', 600) + "\"}}";
            await h.ClientSendsAsync(big, TestTimeout);
            Assert.Equal(big, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.OversizedFrame);
        }

        [Fact]
        public async Task ResponseWithNullIdRoutesToLegacyWithDiagnostic()
        {
            await using var h = new MuxHarness();
            string json = """{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"parse error"}}""";
            await h.ClientSendsAsync(json, TestTimeout);
            Assert.Equal(json, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }

        [Fact]
        public async Task RoutingNeverDeserializesBeyondTopLevel()
        {
            // A nested object containing "method" must not confuse routing: this is a
            // response (no top-level method) to an unknown id -> legacy + diagnostic.
            await using var h = new MuxHarness();
            string json = """{"jsonrpc":"2.0","id":"u-1","result":{"method":"v2/decoy","id":42}}""";
            await h.ClientSendsAsync(json, TestTimeout);
            Assert.Equal(json, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Contains(h.Diagnostics, d => d.Code == MultiplexerDiagnosticCodes.UnknownResponseId);
        }

        [Fact]
        public async Task NotificationsWithoutIdRouteByMethodPrefix()
        {
            await using var h = new MuxHarness();
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"v2/query.ack","params":{"queryId":"q-1","pageSeq":3}}""", TestTimeout);
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":12}}""", TestTimeout);
            Assert.Contains("v2/query.ack", await h.Sts2ReceivesAsync(TestTimeout));
            Assert.Contains("$/cancelRequest", await h.LegacyReceivesAsync(TestTimeout));
        }

        [Fact]
        public async Task OutboundResponsesAndNotificationsPassThroughUnchanged()
        {
            await using var h = new MuxHarness();
            string response = """{"jsonrpc":"2.0","id":5,"result":{"ok":true}}""";
            string notification = """{"jsonrpc":"2.0","method":"v2/query.rows","params":{"queryId":"q","rows":[[1]]}}""";
            await h.LegacySendsAsync(response, TestTimeout);
            await h.Sts2SendsAsync(notification, TestTimeout);

            string first = await h.StdoutFrameAsync(TestTimeout);
            string second = await h.StdoutFrameAsync(TestTimeout);
            Assert.Contains(response, new[] { first, second });
            Assert.Contains(notification, new[] { first, second });
        }
    }
}
