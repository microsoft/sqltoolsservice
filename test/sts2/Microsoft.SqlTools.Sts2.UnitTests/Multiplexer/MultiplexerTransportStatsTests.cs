//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    public class MultiplexerTransportStatsTests
    {
        private static CancellationToken TestTimeout => new CancellationTokenSource(System.TimeSpan.FromSeconds(10)).Token;

        [Fact]
        public async Task DisposalEmitsContentFreeAggregateTransportStats()
        {
            var h = new MuxHarness();
            string privateValue = "private-canary-" + new string('x', 100_000);
            string notification =
                "{\"jsonrpc\":\"2.0\",\"method\":\"v2/query.rows\",\"params\":{\"value\":"
                + JsonSerializer.Serialize(privateValue)
                + "}}";

            Task send = h.Sts2SendsAsync(notification, TestTimeout);
            Assert.Equal(notification, await h.StdoutFrameAsync(TestTimeout));
            await send;
            await h.DisposeAsync();

            MultiplexerDiagnostic diagnostic = Assert.Single(
                h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));
            Assert.DoesNotContain("private-canary", diagnostic.Message);

            using JsonDocument document = JsonDocument.Parse(diagnostic.Message);
            JsonElement root = document.RootElement;
            Assert.Equal("sts2.transport.stats/1", root.GetProperty("schema").GetString());

            JsonElement stats = root.GetProperty("sts2");
            Assert.Equal(1, stats.GetProperty("outboundFrames").GetInt64());
            Assert.True(stats.GetProperty("outboundFrameBytes").GetInt64() > privateValue.Length);
            Assert.Equal(1, stats.GetProperty("largeObjectFrames").GetInt64());
            Assert.Equal(
                stats.GetProperty("outboundFrames").GetInt64(),
                stats.GetProperty("directFrames").GetInt64() + stats.GetProperty("materializedFrames").GetInt64());
            Assert.Equal(
                stats.GetProperty("materializedFrames").GetInt64(),
                stats.GetProperty("reusableFrames").GetInt64() + stats.GetProperty("pooledFrames").GetInt64());
            Assert.Equal(
                stats.GetProperty("materializedBytes").GetInt64(),
                stats.GetProperty("reusableBytes").GetInt64() + stats.GetProperty("pooledBytes").GetInt64());
            Assert.Equal(stats.GetProperty("materializedBytes").GetInt64(), stats.GetProperty("bufferClearBytes").GetInt64());
            Assert.Equal(stats.GetProperty("pooledClearBytes").GetInt64(), stats.GetProperty("pooledBytes").GetInt64());
            Assert.Equal(1, stats.GetProperty("stdoutWriteCalls").GetInt64());
            Assert.Equal(1, stats.GetProperty("stdoutFlushCalls").GetInt64());
            Assert.Equal(
                stats.GetProperty("outboundFrames").GetInt64(),
                stats.GetProperty("singleSegmentFrames").GetInt64() + stats.GetProperty("multiSegmentFrames").GetInt64());
        }

        [Fact]
        public async Task ShutdownEmitsStatsBeforeLegacyCanTerminateProcessAndOnlyOnce()
        {
            var h = new MuxHarness(lifecycleSink: new TestLifecycleSink());
            const string shutdown = """{"jsonrpc":"2.0","id":9,"method":"shutdown"}""";

            await h.ClientSendsAsync(shutdown, TestTimeout);
            Assert.Equal(shutdown, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Single(h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));

            await h.DisposeAsync();
            Assert.Single(h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));
        }

        [Fact]
        public async Task QueryCompletionCheckpointsStatsBeforeProcessShutdown()
        {
            var h = new MuxHarness();
            const string complete = """{"jsonrpc":"2.0","method":"v2/query.complete","params":{"status":"succeeded"}}""";

            await h.Sts2SendsAsync(complete, TestTimeout);
            Assert.Equal(complete, await h.StdoutFrameAsync(TestTimeout));
            Assert.True(SpinWait.SpinUntil(
                () => h.Diagnostics.Any(item => item.Code == MultiplexerDiagnosticCodes.TransportStats),
                millisecondsTimeout: 1_000));
            Assert.Single(h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));

            await h.DisposeAsync();
            MultiplexerDiagnostic diagnostic = Assert.Single(
                h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));
            using JsonDocument document = JsonDocument.Parse(diagnostic.Message);
            JsonElement stats = document.RootElement.GetProperty("sts2");
            Assert.Equal(1, stats.GetProperty("directFrames").GetInt64());
            Assert.Equal(0, stats.GetProperty("materializedFrames").GetInt64());
        }

        [Fact]
        public async Task FragmentedFrameUsesAndClearsReusableContiguousBuffer()
        {
            var h = new MuxHarness();
            string notification =
                "{\"jsonrpc\":\"2.0\",\"method\":\"v2/query.rows\",\"params\":{\"value\":\""
                + new string('x', 100_000)
                + "\"}}";

            byte[] frame = Frames.Frame(notification);
            Task send = Task.Run(async () =>
            {
                for (int offset = 0; offset < frame.Length; offset += 1_024)
                {
                    int count = Math.Min(1_024, frame.Length - offset);
                    await h.Mux.Sts2Output.WriteAsync(frame.AsMemory(offset, count), TestTimeout);
                    await h.Mux.Sts2Output.FlushAsync(TestTimeout);
                }
            });
            Assert.Equal(notification, await h.StdoutFrameAsync(TestTimeout));
            await send;
            await h.DisposeAsync();

            MultiplexerDiagnostic diagnostic = Assert.Single(
                h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));
            using JsonDocument document = JsonDocument.Parse(diagnostic.Message);
            JsonElement stats = document.RootElement.GetProperty("sts2");
            Assert.Equal(1, stats.GetProperty("multiSegmentFrames").GetInt64());
            Assert.Equal(1, stats.GetProperty("reusableFrames").GetInt64());
            Assert.Equal(1, stats.GetProperty("reusableBufferAllocations").GetInt64());
            Assert.Equal(stats.GetProperty("reusableBytes").GetInt64(), stats.GetProperty("bufferClearBytes").GetInt64());
            Assert.Equal(0, stats.GetProperty("pooledFrames").GetInt64());
            Assert.Equal(0, stats.GetProperty("directFrames").GetInt64());
        }

        [Fact]
        public async Task FragmentedFrameAboveReusableLimitUsesAndClearsPooledBuffer()
        {
            var h = new MuxHarness();
            string notification =
                "{\"jsonrpc\":\"2.0\",\"method\":\"v2/query.rows\",\"params\":{\"value\":\""
                + new string('x', 1_100_000)
                + "\"}}";

            byte[] frame = Frames.Frame(notification);
            Task send = Task.Run(async () =>
            {
                for (int offset = 0; offset < frame.Length; offset += 16_384)
                {
                    int count = Math.Min(16_384, frame.Length - offset);
                    await h.Mux.Sts2Output.WriteAsync(frame.AsMemory(offset, count), TestTimeout);
                    await h.Mux.Sts2Output.FlushAsync(TestTimeout);
                }
            });
            Assert.Equal(notification, await h.StdoutFrameAsync(TestTimeout));
            await send;
            await h.DisposeAsync();

            MultiplexerDiagnostic diagnostic = Assert.Single(
                h.Diagnostics.Where(item => item.Code == MultiplexerDiagnosticCodes.TransportStats));
            using JsonDocument document = JsonDocument.Parse(diagnostic.Message);
            JsonElement stats = document.RootElement.GetProperty("sts2");
            Assert.Equal(1, stats.GetProperty("multiSegmentFrames").GetInt64());
            Assert.Equal(1, stats.GetProperty("pooledFrames").GetInt64());
            Assert.Equal(stats.GetProperty("pooledBytes").GetInt64(), stats.GetProperty("pooledClearBytes").GetInt64());
            Assert.Equal(stats.GetProperty("pooledBytes").GetInt64(), stats.GetProperty("bufferClearBytes").GetInt64());
            Assert.Equal(0, stats.GetProperty("reusableFrames").GetInt64());
            Assert.Equal(0, stats.GetProperty("directFrames").GetInt64());
        }
    }
}
