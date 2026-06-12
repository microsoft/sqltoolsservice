//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Architecture;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Scenarios
{
    /// <summary>
    /// Produces the persistent journal corpus under <c>artifacts/test-journals/</c> that
    /// verify.sh batch-verifies with <c>sts2-replay verify</c> (SPEC §15.1 step 6), and
    /// asserts the secret-canary and replay gates inline so a broken corpus fails here
    /// first.
    /// </summary>
    public class VerificationCorpusProducer
    {
        private sealed class ImmediateToyEffectRunner : ISts2EffectRunner
        {
            public void Run(EffectWorkItem effect, ICoordinatorInbox inbox) =>
                _ = Task.Run(() => inbox.PostEffectResponseAsync(effect.EffectId, effect.EffectName, effect.Args, effect.CauseSeq).AsTask());
        }

        [Fact]
        public async Task ProduceToySessionJournalForVerifyGate()
        {
            string directory = Path.Combine(RepoRoot.Path, "artifacts", "test-journals", "toy-session");
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true); // regenerate fresh each run
            }

            var emitted = new ConcurrentQueue<OutboundRpcMessage>();
            await using (var coordinator = new Coordinator(
                new JournalWriter("toy-session", new JournalOptions { Directory = directory },
                    new JournalRunInfo { ServiceVersion = "verify-corpus", CommandLine = ["--enable-sts2"] }),
                new CoordinatorOptions { RunId = "toy-session" },
                new ImmediateToyEffectRunner(),
                emitted.Enqueue))
            {
                await coordinator.PostRpcRequestAsync("v2/toy.echo", "r-1", JsonDocument.Parse("""{"text":"verify"}""").RootElement);
                await coordinator.PostRpcRequestAsync("v2/toy.effect", "r-2", JsonDocument.Parse("""{"value":1}""").RootElement);
                await coordinator.PostRpcRequestAsync("v2/toy.bogus", "r-3", null);
                for (int spins = 0; emitted.Count < 3 && spins < 500; spins++)
                {
                    await Task.Delay(10);
                }
            }

            // The corpus must replay identically (I7) and be canary-clean (I6) at birth.
            Assert.True(JournalReplayer.Replay(JournalReader.ReadAll(directory)).Identical);
            Assert.Empty(SecretCanaries.ScanDirectory(directory));
        }
    }
}
