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
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>
    /// SPEC §8.2/§8.4: digest capture elides row cells and SQL text from the journal
    /// (authoritative-digest wrappers) while the wire still carries the real data, and
    /// replay stays fully digest-identical (I7 in digest mode).
    /// </summary>
    public sealed class DigestCaptureTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-capture-test-" + Guid.NewGuid().ToString("N"));

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
        public async Task DigestModeElidesJournalKeepsWireAndReplaysIdentically()
        {
            const string SecretishSql = "select * from VerySensitiveTable where x = 'PRIVATE-LITERAL'";
            List<OutboundRpcMessage> wireRows;

            await using (var session = new Sts2TestSession(directory, "digest-capture", rowCapture: "digest", sqlCapture: "digest"))
            {
                string connectionId = await session.OpenConnectionAsync();
                await session.RequestAsync("v2/query.execute",
                    $$"""{"connectionId":"{{connectionId}}","sql":"{{SecretishSql}}"}""");
                await session.WaitForNotificationsAsync("v2/query.complete", 1);
                wireRows = await session.WaitForNotificationsAsync("v2/query.rows", 1);
                await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            }

            // The WIRE carried real cells (clients must get data).
            Assert.Contains("s0-1", wireRows[0].Body!.Value.GetProperty("rows").GetRawText());

            // The JOURNAL carries neither SQL text nor cell values, only wrappers.
            string journalText = string.Join("\n",
                Directory.EnumerateFiles(directory, "*.jsonl").Select(File.ReadAllText));
            Assert.DoesNotContain("VerySensitiveTable", journalText);
            Assert.DoesNotContain("PRIVATE-LITERAL", journalText);
            Assert.DoesNotContain("s0-1", journalText);
            Assert.Contains("\"$redacted\"", journalText);

            // Replay is digest-identical even in digest mode: Core relays wrappers
            // deterministically, and digests are computed over the elided payloads.
            ReplayResult replay = JournalReplayer.Replay(JournalReader.ReadAll(directory));
            Assert.True(replay.Identical,
                "divergence: " + replay.Divergence?.Recorded + " vs " + replay.Divergence?.Replayed);

            // The wrappers carry the original digests (verifiable without the text).
            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Sts2Envelope rowsEnvelope = journal.First(e => e.Type == "driver.queryEvent"
                && e.Payload!.Value.TryGetProperty("eventType", out var et) && et.GetString() == "rows");
            var wrapper = rowsEnvelope.Payload!.Value.GetProperty("rows");
            Assert.True(wrapper.GetProperty("$redacted").GetBoolean());
            Assert.Matches("^sha256:[0-9a-f]{64}$", wrapper.GetProperty("digest").GetString()!);
            Assert.Equal(3, wrapper.GetProperty("rows").GetInt32());
        }

        [Fact]
        public async Task FullModeKeepsEverythingInline()
        {
            await using (var session = new Sts2TestSession(directory, "full-capture"))
            {
                string connectionId = await session.OpenConnectionAsync();
                await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
                await session.WaitForNotificationsAsync("v2/query.complete", 1);
            }

            string journalText = string.Join("\n",
                Directory.EnumerateFiles(directory, "*.jsonl").Select(File.ReadAllText));
            Assert.Contains("select 1", journalText);
            Assert.Contains("s0-1", journalText); // full row cells inline
        }
    }
}
