//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
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

        /// <summary>A representative session: open, query, error open, close.</summary>
        private async Task<List<Sts2Envelope>> ProduceJournalAsync()
        {
            await using (var session = new Sts2TestSession(directory, "run-replay"))
            {
                session.Driver.EnqueueOpen(new FakeOpenBehavior()); // ok
                string connectionId = await session.OpenConnectionAsync("open-1");
                await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
                await session.WaitForNotificationsAsync("v2/query.complete", 1);

                session.Driver.EnqueueOpen(new FakeOpenBehavior { Outcome = "authFail" });
                await session.RequestAsync("v2/connection.open", Sts2TestSession.OpenPayload("open-2"));
                await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
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
            Assert.Empty(result.FinalState.Connections);
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
        }

        [Fact]
        public async Task TamperedPayloadWithOriginalDigestIsDetected()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            int index = journal.FindIndex(e => e.Kind == EnvelopeKinds.RpcInRequest);
            JsonElement replacement = JsonDocument.Parse("""{"echo":"tampered"}""").RootElement.Clone();
            journal[index] = journal[index] with { Payload = replacement };

            ReplayResult result = JournalReplayer.Replay(journal);

            Assert.Equal(ReplayOutcome.Diverged, result.Outcome);
            Assert.Equal(journal[index].Seq, result.Divergence!.Seq);
            Assert.Contains("payload digest mismatch", result.Divergence.Recorded);
        }

        [Fact]
        public async Task DuplicateAndGappedSequencesAreDetected()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();

            List<Sts2Envelope> duplicate = journal.ToList();
            duplicate[1] = duplicate[1] with { Seq = duplicate[0].Seq };
            AssertInvalidJournal(duplicate, "non-gapless sequence");

            List<Sts2Envelope> gapped = journal.ToList();
            gapped[1] = gapped[1] with { Seq = gapped[1].Seq + 1 };
            AssertInvalidJournal(gapped, "non-gapless sequence");
        }

        [Fact]
        public async Task MixedRunIdsAndUnknownSchemasAreDetected()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();

            List<Sts2Envelope> mixedRun = journal.ToList();
            mixedRun[1] = mixedRun[1] with { RunId = "another-run" };
            AssertInvalidJournal(mixedRun, "mixed or empty run id");

            List<Sts2Envelope> unknownSchema = journal.ToList();
            unknownSchema[0] = unknownSchema[0] with { Schema = "sts2.envelope/999" };
            AssertInvalidJournal(unknownSchema, "unknown schema");
        }

        [Fact]
        public async Task MissingInvalidAndIncorrectCausesAreDetected()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            int outputIndex = journal.FindIndex(e =>
                e.Kind is EnvelopeKinds.RpcOutResult or EnvelopeKinds.RpcOutError
                && e.Cause is not null);
            Assert.True(outputIndex >= 0);

            List<Sts2Envelope> missingCause = journal.ToList();
            missingCause[outputIndex] = missingCause[outputIndex] with { Cause = null };
            AssertInvalidJournal(missingCause, "has no cause");

            List<Sts2Envelope> futureCause = journal.ToList();
            futureCause[outputIndex] = futureCause[outputIndex] with { Cause = futureCause[outputIndex].Seq };
            AssertInvalidJournal(futureCause, "invalid cause");

            int laterOutputIndex = journal.FindIndex(e =>
                e.Kind is EnvelopeKinds.RpcOutResult or EnvelopeKinds.RpcOutError or EnvelopeKinds.EffectRequest
                && e.Cause is long cause
                && cause != 1);
            Assert.True(laterOutputIndex >= 0);
            List<Sts2Envelope> incorrectCause = journal.ToList();
            incorrectCause[laterOutputIndex] = incorrectCause[laterOutputIndex] with { Cause = 1 };
            ReplayResult incorrect = JournalReplayer.Replay(incorrectCause);
            Assert.Equal(ReplayOutcome.Diverged, incorrect.Outcome);
            Assert.Contains("cause=1", incorrect.Divergence!.Recorded);
        }

        [Fact]
        public async Task TruncatedJournalIsIncompleteNotIdentical() // R006
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();

            // A complete journal verifies.
            Assert.Equal(ReplayOutcome.Verified, JournalReplayer.Replay(journal).Outcome);

            // Truncating mid-output must be caught: strict replay reports Incomplete, never
            // Verified/Identical. (A cut landing exactly on a complete boundary may verify —
            // that is a legitimately complete prefix.) Crucially, a Verified prefix must have
            // an empty pending queue, and an Incomplete one must never claim Identical.
            bool sawIncomplete = false;
            for (int cut = 1; cut < journal.Count; cut++)
            {
                ReplayResult partial = JournalReplayer.Replay(journal.Take(cut).ToList());
                if (partial.Outcome == ReplayOutcome.Verified)
                {
                    Assert.Equal(0, partial.PendingOutputCount);
                }
                else if (partial.Outcome == ReplayOutcome.Incomplete)
                {
                    Assert.False(partial.Identical);
                    Assert.True(partial.PendingOutputCount > 0);
                    sawIncomplete = true;
                }
            }
            Assert.True(sawIncomplete, "expected at least one mid-output truncation to be flagged Incomplete");
        }

        [Fact]
        public async Task TamperedCorrelationIsDetected() // R006 (corr was previously ignored)
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();
            int index = journal.FindIndex(e => e.Kind == "rpc.out.result" && e.Corr is not null);
            journal[index] = journal[index] with { Corr = "r-tampered" };

            ReplayResult result = JournalReplayer.Replay(journal);
            Assert.Equal(ReplayOutcome.Diverged, result.Outcome);
            Assert.Equal(journal[index].Seq, result.Divergence!.Seq);
        }

        [Fact]
        public async Task UntilReturnsStateAtRequestedSeq()
        {
            List<Sts2Envelope> journal = await ProduceJournalAsync();

            // Find the seq right after the first open completed: one open connection.
            Sts2Envelope openResult = journal.First(e => e.Kind == "rpc.out.result" && e.Payload!.Value.TryGetProperty("connectionId", out _));
            ReplayResult mid = JournalReplayer.Replay(journal, untilSeq: openResult.Seq);
            Assert.Single(mid.FinalState.Connections);

            ReplayResult full = JournalReplayer.Replay(journal);
            Assert.Empty(full.FinalState.Connections);
        }

        [Fact]
        public async Task ReplayNeverReExecutesEffects()
        {
            // Replay feeds recorded effect.res envelopes back through the reducer; no
            // driver exists at replay time, so any re-execution attempt would be loud.
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
            JsonDocument.Parse(dump1);
            Assert.Empty(SecretCanaries.FindIn(dump1)); // I16
        }

        private static void AssertInvalidJournal(
            IReadOnlyList<Sts2Envelope> journal,
            string expectedProblem)
        {
            ReplayResult result = JournalReplayer.Replay(journal);
            Assert.Equal(ReplayOutcome.Diverged, result.Outcome);
            Assert.Contains("invalid journal", result.Divergence!.Recorded);
            Assert.Contains(expectedProblem, result.Divergence.Recorded);
        }
    }
}
