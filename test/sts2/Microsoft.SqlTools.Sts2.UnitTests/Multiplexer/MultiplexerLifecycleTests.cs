//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Multiplexer
{
    /// <summary>
    /// SPEC §6.2 / I14: shutdown and exit are lifecycle-mirrored to STS2, never
    /// raw-broadcast, and exit waits (bounded) for the STS2 journal flush.
    /// </summary>
    public class MultiplexerLifecycleTests
    {
        private static CancellationToken TestTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        [Fact]
        public async Task ShutdownRoutesRawToLegacyAndMirrorsToSink()
        {
            var sink = new TestLifecycleSink();
            await using var h = new MuxHarness(lifecycleSink: sink);

            string shutdown = """{"jsonrpc":"2.0","id":40,"method":"shutdown"}""";
            await h.ClientSendsAsync(shutdown, TestTimeout);
            Assert.Equal(shutdown, await h.LegacyReceivesAsync(TestTimeout));
            Assert.Equal(1, sink.ShutdownCalls);

            // STS2 must NOT receive the shutdown request (no duplicate response possible).
            // Marker: the next v2 frame must be the first thing STS2 sees.
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":41,"method":"v2/marker"}""", TestTimeout);
            Assert.Contains("v2/marker", await h.Sts2ReceivesAsync(TestTimeout));
        }

        [Fact]
        public async Task ExitWaitsForSts2FlushBeforeForwardingToLegacy()
        {
            var sink = new TestLifecycleSink { CompleteExitImmediately = false };
            await using var h = new MuxHarness(lifecycleSink: sink);

            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"exit"}""", TestTimeout);

            // Legacy must not see exit while the flush is pending.
            Task<string> legacyRead = h.LegacyReceivesAsync(TestTimeout);
            await Task.Delay(100);
            Assert.False(legacyRead.IsCompleted, "exit reached legacy before STS2 flush completed");
            Assert.Equal(1, sink.ExitCalls);

            sink.CompleteExitFlush();
            Assert.Contains("exit", await legacyRead);
        }

        [Fact]
        public async Task ExitForwardsAfterBoundedTimeoutWhenFlushHangs()
        {
            var sink = new TestLifecycleSink { CompleteExitImmediately = false }; // never completed
            await using var h = new MuxHarness(
                new MultiplexerOptions { ExitFlushMilliseconds = 250 },
                lifecycleSink: sink);

            var sw = Stopwatch.StartNew();
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"exit"}""", TestTimeout);
            Assert.Contains("exit", await h.LegacyReceivesAsync(TestTimeout));
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds >= 200, $"exit forwarded too early ({sw.ElapsedMilliseconds}ms): flush wait was skipped");
            Assert.True(sw.ElapsedMilliseconds < 5000, $"exit took {sw.ElapsedMilliseconds}ms: bounded wait not enforced");
        }

        [Fact]
        public async Task LifecycleWithoutSinkStillForwardsToLegacy()
        {
            await using var h = new MuxHarness(); // no sink registered
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":1,"method":"shutdown"}""", TestTimeout);
            Assert.Contains("shutdown", await h.LegacyReceivesAsync(TestTimeout));
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","method":"exit"}""", TestTimeout);
            Assert.Contains("exit", await h.LegacyReceivesAsync(TestTimeout));
        }

        [Fact]
        public async Task StdinEofCompletesChannelInputs()
        {
            await using var h = new MuxHarness();
            await h.ClientSendsAsync("""{"jsonrpc":"2.0","id":1,"method":"legacy/x"}""", TestTimeout);
            await h.LegacyReceivesAsync(TestTimeout);

            h.ClientClosesStdin();
            await Assert.ThrowsAsync<System.IO.EndOfStreamException>(() => h.LegacyReceivesAsync(TestTimeout));
            await Assert.ThrowsAsync<System.IO.EndOfStreamException>(() => h.Sts2ReceivesAsync(TestTimeout));
        }
    }
}
