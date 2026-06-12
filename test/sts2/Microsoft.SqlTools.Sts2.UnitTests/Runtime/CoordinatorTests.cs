//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §9.1: journal-before-dispatch, gapless seq, causal chains (I5).</summary>
    public sealed class CoordinatorTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-coord-test-" + Guid.NewGuid().ToString("N"));

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

        [Fact]
        public async Task PingIsJournaledWriteAheadThenEmitted()
        {
            await using (var session = new Sts2TestSession(directory))
            {
                OutboundRpcMessage ping = await session.RequestAsync("v2/diagnostics.ping", """{"echo":"hello"}""");
                Assert.Equal("rpc.out.result", ping.Kind);
                Assert.Equal("hello", ping.Body!.Value.GetProperty("echo").GetString());
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Assert.Equal(["control", "rpc.in.request", "rpc.out.result"], journal.Select(e => e.Kind));
            Assert.Equal([1L, 2L, 3L], journal.Select(e => e.Seq));
            Assert.Null(journal[0].Cause);
            Assert.Equal(2, journal[2].Cause);
            Assert.All(journal, e => Assert.Matches("^sha256:[0-9a-f]{64}$", e.Digest));
        }

        [Fact]
        public async Task EffectFlowChainsCausesThroughTheJournal()
        {
            await using (var session = new Sts2TestSession(directory))
            {
                await session.OpenConnectionAsync();
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Assert.Equal(["control", "rpc.in.request", "effect.req", "effect.res", "rpc.out.result"], journal.Select(e => e.Kind));
            Assert.Equal(2, journal[2].Cause);  // effect.req caused by the open request
            Assert.Equal(3, journal[3].Cause);  // effect.res caused by effect.req
            Assert.Equal(4, journal[4].Cause);  // result caused by effect.res
            Assert.Equal("drv-open-2", journal[2].Corr);
        }

        [Fact]
        public async Task SeqIsGaplessAcrossMixedTraffic()
        {
            await using (var session = new Sts2TestSession(directory))
            {
                for (int i = 0; i < 10; i++)
                {
                    await session.RequestAsync("v2/diagnostics.ping", $$"""{"echo":"{{i}}"}""");
                    await session.NotifyAsync("v2/query.ack", """{"queryId":"q-unknown"}""");
                }
            }

            List<long> seqs = JournalReader.ReadAll(directory).Select(e => e.Seq).ToList();
            Assert.Equal(Enumerable.Range(1, seqs.Count).Select(i => (long)i), seqs); // I5: gapless
        }

        [Fact]
        public async Task UnknownMethodJournalsAndEmitsStableError()
        {
            await using (var session = new Sts2TestSession(directory))
            {
                OutboundRpcMessage error = await session.RequestAsync("v2/nope.bogus", null);
                Assert.Equal("rpc.out.error", error.Kind);
                Assert.Equal("Sts2.InvalidRequest", error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
                Assert.Equal(System.Text.Json.JsonValueKind.Number, error.Body!.Value.GetProperty("code").ValueKind); // I12
            }
            Assert.Contains(JournalReader.ReadAll(directory), e => e.Kind == "rpc.out.error");
        }

        [Fact]
        public async Task ControlSignalReachesCoreState()
        {
            await using var session = new Sts2TestSession(directory);
            await session.Coordinator.PostControlAsync("lifecycle.shutdown");
            for (int spins = 0; !session.Coordinator.CurrentState.ShuttingDown && spins < 500; spins++)
            {
                await Task.Delay(10);
            }
            Assert.True(session.Coordinator.CurrentState.ShuttingDown);
        }
    }
}
