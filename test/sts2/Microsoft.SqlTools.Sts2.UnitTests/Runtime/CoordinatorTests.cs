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

        private sealed class ImmediateToyEffectRunner : ISts2EffectRunner
        {
            public void Run(EffectWorkItem effect, ICoordinatorInbox inbox) =>
                _ = Task.Run(() => inbox.PostEffectResponseAsync(effect.EffectId, effect.EffectName, effect.Args, effect.CauseSeq).AsTask());
        }

        private sealed class Harness : IAsyncDisposable
        {
            public readonly ConcurrentQueue<OutboundRpcMessage> Emitted = new();
            public readonly Coordinator Coordinator;
            public readonly string Directory;

            public Harness(string directory)
            {
                Directory = directory;
                Coordinator = new Coordinator(
                    new JournalWriter("run-coord", new JournalOptions { Directory = directory }, new JournalRunInfo { ServiceVersion = "0" }),
                    new CoordinatorOptions { RunId = "run-coord" },
                    new ImmediateToyEffectRunner(),
                    Emitted.Enqueue);
            }

            public async Task<OutboundRpcMessage> WaitForEmittedAsync(int count)
            {
                for (int spins = 0; Emitted.Count < count && spins < 500; spins++)
                {
                    await Task.Delay(10);
                }
                Assert.True(Emitted.Count >= count, $"expected {count} emitted messages, saw {Emitted.Count}");
                return Emitted.Last();
            }

            public ValueTask DisposeAsync() => Coordinator.DisposeAsync();
        }

        [Fact]
        public async Task EchoIsJournaledWriteAheadThenEmitted()
        {
            await using (var h = new Harness(directory))
            {
                await h.Coordinator.PostRpcRequestAsync("v2/toy.echo", "r-1", JsonDocument.Parse("""{"text":"hello"}""").RootElement);
                OutboundRpcMessage emitted = await h.WaitForEmittedAsync(1);
                Assert.Equal("rpc.out.result", emitted.Kind);
                Assert.Equal("r-1", emitted.Corr);
                Assert.Equal("hello", emitted.Body!.Value.GetProperty("echo").GetString());
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Assert.Equal(2, journal.Count);
            Assert.Equal(["rpc.in.request", "rpc.out.result"], journal.Select(e => e.Kind));
            Assert.Equal([1L, 2L], journal.Select(e => e.Seq));
            Assert.Null(journal[0].Cause);
            Assert.Equal(1, journal[1].Cause);
            Assert.All(journal, e => Assert.Matches("^sha256:[0-9a-f]{64}$", e.Digest));
        }

        [Fact]
        public async Task EffectFlowChainsCausesThroughTheJournal()
        {
            await using (var h = new Harness(directory))
            {
                await h.Coordinator.PostRpcRequestAsync("v2/toy.effect", "r-2", JsonDocument.Parse("""{"value":42}""").RootElement);
                OutboundRpcMessage final = await h.WaitForEmittedAsync(1);
                Assert.Equal("rpc.out.result", final.Kind);
                Assert.Equal("r-2", final.Corr);
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Assert.Equal(["rpc.in.request", "effect.req", "effect.res", "rpc.out.result"], journal.Select(e => e.Kind));
            Assert.Equal([1L, 2, 3, 4], journal.Select(e => e.Seq));
            Assert.Equal(1, journal[1].Cause);  // effect.req caused by the request
            Assert.Equal(2, journal[2].Cause);  // effect.res caused by effect.req
            Assert.Equal(3, journal[3].Cause);  // result caused by effect.res
            Assert.Equal("eff-1", journal[1].Corr);
        }

        [Fact]
        public async Task SeqIsGaplessAcrossMixedTraffic()
        {
            await using (var h = new Harness(directory))
            {
                for (int i = 0; i < 10; i++)
                {
                    await h.Coordinator.PostRpcRequestAsync("v2/toy.echo", "r-" + i, JsonDocument.Parse("""{"text":"x"}""").RootElement);
                    await h.Coordinator.PostRpcNotificationAsync("v2/toy.note", null);
                }
                await h.WaitForEmittedAsync(10);
            }

            List<long> seqs = JournalReader.ReadAll(directory).Select(e => e.Seq).ToList();
            Assert.Equal(Enumerable.Range(1, seqs.Count).Select(i => (long)i), seqs); // I5: gapless
        }

        [Fact]
        public async Task UnknownMethodJournalsAndEmitsStableError()
        {
            await using (var h = new Harness(directory))
            {
                await h.Coordinator.PostRpcRequestAsync("v2/toy.bogus", "r-9", null);
                OutboundRpcMessage error = await h.WaitForEmittedAsync(1);
                Assert.Equal("rpc.out.error", error.Kind);
                Assert.Equal("Sts2.InvalidRequest", error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
                Assert.Equal(JsonValueKind.Number, error.Body!.Value.GetProperty("code").ValueKind); // I12
            }

            Assert.Contains(JournalReader.ReadAll(directory), e => e.Kind == "rpc.out.error");
        }

        [Fact]
        public async Task ControlSignalReachesCoreState()
        {
            await using var h = new Harness(directory);
            await h.Coordinator.PostControlAsync("lifecycle.shutdown");
            for (int spins = 0; !h.Coordinator.CurrentState.ShuttingDown && spins < 500; spins++)
            {
                await Task.Delay(10);
            }
            Assert.True(h.Coordinator.CurrentState.ShuttingDown);
        }
    }
}
