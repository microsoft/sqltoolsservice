//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Architecture;
using Microsoft.SqlTools.Sts2.UnitTests.Runtime;
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
        [Fact]
        public async Task ProduceSessionJournalForVerifyGate()
        {
            string directory = Path.Combine(RepoRoot.Path, "artifacts", "test-journals", "verify-session");
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true); // regenerate fresh each run
            }

            await using (var session = new Sts2TestSession(directory, "verify-session"))
            {
                await session.RequestAsync("v2/initialize", """{"clientName":"verify-corpus"}""");
                string connectionId = await session.OpenConnectionAsync("open-1");
                await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
                await session.WaitForNotificationsAsync("v2/query.complete", 1);
                await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
                await session.RequestAsync("v2/nope.unknown", null); // an error in the corpus too
            }

            // The corpus must replay identically (I7) and be canary-clean (I6) at birth.
            Assert.True(JournalReplayer.Replay(JournalReader.ReadAll(directory)).Identical);
            Assert.Empty(SecretCanaries.ScanDirectory(directory));
        }
    }
}
