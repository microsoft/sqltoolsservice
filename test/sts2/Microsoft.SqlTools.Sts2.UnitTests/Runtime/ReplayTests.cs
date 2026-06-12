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
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §13 / I7: journal in -> identical outbound digest sequence out.</summary>
    public sealed class ReplayTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-replay-test-" + Guid.NewGuid().ToString("N"));

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

        /// <summary>Runs a representative toy session through the live coordinator and returns its journal.</summary>
        private async Task<List<Sts2Envelope>> ProduceJournalAsync()
        {
            var emitted = new ConcurrentQueue<OutboundRpcMessage>();
            await using (var coordinator = new Coordinator(
                new JournalWriter("run-replay", new JournalOptions { Directory = directory }, new JournalRunInfo { ServiceVersion = "0" }),
                new CoordinatorOptions { RunId = "run-replay" },
                new ImmediateToyEffectRunner(),
                emitted.Enqueue))
            {
                await coordinator.PostRpcRequestAsync("v2/toy.echo", "r-1", JsonDocument.Parse("""{"text":"alpha"}""").RootElement);
                await coordinator.PostRpcRequestAsync("v2/toy.effect", "r-2", JsonDocument.Parse("""{"value":7}""").RootElement);
                await coordinator.PostRpcRequestAsync("v2/toy.bogus", "r-3", null);
                await coordinator.PostRpcRequestAsync("v2/toy.echo", "r-4", JsonDocument.Parse("""{"text":"omega"}""").RootElement);
                for (int spins = 0; emitted.Count < 4 && spins < 500; spins++)
                {
                    await Task.Delay(10);
                }
                Assert.Equal(4, emitted.Count);
            }
            return JournalReader.ReadAll(directory).ToList();
        }

        [Fact]
        public async Task ReplayReproducesIdenticalOutboundDigestSequence()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            ReplayResult result = JournalReplayer.Replay(journal);

            Assert.True(result.Identical, "divergence: " + result.Divergence?.Recorded + " vs " + result.Divergence?.Replayed);
            string[] recordedOutbound = journal
                .Where(e => e.Kind is "rpc.out.result" or "rpc.out.error" or "rpc.out.notify")
                .Select(e => e.Digest).ToArray();
            Assert.Equal(recordedOutbound, result.OutboundDigests);
            Assert.Equal(2, result.FinalState.ToyCounter);
        }

        [Fact]
        public async Task TamperedOutputIsDetectedWithCauseChain()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            int index = journal.FindIndex(e => e.Kind == "rpc.out.result");
            journal[index] = journal[index] with { Digest = "sha256:" + new string('0', 64) };

            ReplayResult result = JournalReplayer.Replay(journal);

            Assert.False(result.Identical);
            Assert.NotNull(result.Divergence);
            Assert.Equal(journal[index].Seq, result.Divergence.Seq);
            Assert.Contains(journal[index].Seq, result.Divergence.CauseChain);
            Assert.Contains(journal[index].Cause!.Value, result.Divergence.CauseChain);
        }

        [Fact]
        public async Task UntilReturnsStateAtRequestedSeq()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();

            // After the first echo request (seq 1) and its result (seq 2), counter is 1.
            ReplayResult atTwo = JournalReplayer.Replay(journal, untilSeq: 2);
            Assert.Equal(1, atTwo.FinalState.ToyCounter);
            Assert.Equal(2, atTwo.LastSeq);

            ReplayResult full = JournalReplayer.Replay(journal);
            Assert.Equal(2, full.FinalState.ToyCounter);
        }

        [Fact]
        public async Task ReplayNeverReExecutesEffects()
        {
            // No effect runner exists here: replay feeds recorded effect.res envelopes
            // back through the reducer. If replay tried to re-run effects it would have
            // nothing to run them with.
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            Assert.Contains(journal, e => e.Kind == "effect.res");
            Assert.True(JournalReplayer.Replay(journal).Identical);
        }

        [Fact]
        public async Task StateDumpIsDeterministicOrderedJson()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            ReplayResult result = JournalReplayer.Replay(journal);

            string dump1 = JournalReplayer.DumpState(result.FinalState, result.LastSeq);
            string dump2 = JournalReplayer.DumpState(result.FinalState, result.LastSeq);
            Assert.Equal(dump1, dump2);
            JsonDocument.Parse(dump1); // valid JSON
            Assert.Contains("\"toyCounter\":2", dump1);
        }
    }
}
