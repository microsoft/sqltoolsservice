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
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>
    /// Row-pipeline attribution (QO-2): the runner stamps per-page stats
    /// (readMs/creditWaitMs/encodeMs/encodedBytes) onto the JOURNALED rows
    /// event and per-query aggregates onto the completed event; Core surfaces
    /// the aggregate as ONE sts2.query.stats diagnostic. The v2 wire shape is
    /// unchanged, and replay stays byte-identical because stats replay from
    /// the journal.
    /// </summary>
    public sealed class QueryPipelineStatsTests : IAsyncDisposable, IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-qstats-test-" + Guid.NewGuid().ToString("N"));
        private readonly Sts2TestSession session;

        public QueryPipelineStatsTests()
        {
            session = new Sts2TestSession(directory, "qstats-test");
        }

        public ValueTask DisposeAsync() => session.DisposeAsync();

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
        public async Task RowsEventsCarryJournaledStatsButTheWireShapeIsUnchanged()
        {
            string connectionId = await session.OpenConnectionAsync();
            await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            // Wire: v2/query.rows params must NOT grow a stats property.
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 1);
            Assert.False(rows[0].Body!.Value.TryGetProperty("stats", out _));

            // Journal: the rows effect.res payload carries the per-page stats.
            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Sts2Envelope rowsEvent = journal.First(e =>
                e.Kind == "effect.res"
                && e.Type == "driver.queryEvent"
                && e.Payload!.Value.TryGetProperty("eventType", out JsonElement t)
                && t.GetString() == "rows");
            JsonElement pageStats = rowsEvent.Payload!.Value.GetProperty("stats");
            Assert.True(pageStats.GetProperty("rowCount").GetInt32() > 0);
            Assert.True(pageStats.GetProperty("encodedBytes").GetInt64() > 0);
            Assert.True(pageStats.GetProperty("encodeMs").GetDouble() >= 0);
            Assert.True(pageStats.GetProperty("creditWaitMs").GetDouble() >= 0);
            Assert.True(pageStats.GetProperty("readMs").GetDouble() >= 0);
            Assert.True(pageStats.GetProperty("cellSlots").GetInt64() > 0);
            Assert.True(pageStats.GetProperty("nullCells").GetInt64() >= 0);
            Assert.True(pageStats.GetProperty("rowsSerializeMs").GetDouble() >= 0);
            Assert.Equal(0, pageStats.GetProperty("utf8MeasureMs").GetDouble());
            Assert.True(pageStats.GetProperty("nullBitmapMs").GetDouble() >= 0);
            Assert.True(pageStats.GetProperty("pageBodyBuildMs").GetDouble() >= 0);
            Assert.True(pageStats.GetProperty("encodePrepAllocatedBytes").GetInt64() > 0);
        }

        [Fact]
        public async Task CompletedAggregateSurfacesAsOneQueryStatsDiagnostic()
        {
            string connectionId = await session.OpenConnectionAsync();
            OutboundRpcMessage execute = await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            string queryId = execute.Body!.Value.GetProperty("queryId").GetString()!;
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            List<Sts2Envelope> statsDiags = journal
                .Where(e => e.Kind == EnvelopeKinds.Diagnostic && e.Type == "sts2.query.stats")
                .ToList();
            Sts2Envelope diag = Assert.Single(statsDiags);
            JsonElement data = diag.Payload!.Value;
            Assert.Equal(queryId, data.GetProperty("queryId").GetString());
            Assert.Equal("succeeded", data.GetProperty("status").GetString());
            JsonElement stats = data.GetProperty("stats");
            Assert.True(stats.GetProperty("pages").GetInt64() >= 1);
            Assert.True(stats.GetProperty("rows").GetInt64() >= 1);
            Assert.True(stats.GetProperty("encodedBytes").GetInt64() > 0);
            Assert.True(stats.GetProperty("encodeMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("cellSlots").GetInt64() > 0);
            Assert.True(stats.GetProperty("eventPayloadBytes").GetInt64() > 0);
            Assert.True(stats.GetProperty("maxEventPayloadBytes").GetInt64() > 0);
            Assert.True(stats.GetProperty("rowsSerializeMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("utf8MeasureMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("nullBitmapMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("pageBodyBuildMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("eventBuildMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("postBuildMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("postMsTotal").GetDouble() >= 0);
            Assert.True(stats.GetProperty("encodePrepAllocatedBytes").GetInt64() > 0);
            // A correctly pre-sized single-buffer event can add no allocation
            // after the row/body phase; zero is therefore a valid outcome.
            Assert.True(stats.GetProperty("eventBuildAllocatedBytes").GetInt64() >= 0);
            Assert.True(stats.GetProperty("postBuildAllocatedBytes").GetInt64() > 0);

            // No SQL text or cell values ride the diagnostic (privacy canary).
            string raw = data.GetRawText();
            Assert.DoesNotContain("select 1", raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sql", raw, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StatsJournalReplaysIdentically()
        {
            string connectionId = await session.OpenConnectionAsync();
            await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
            await session.DisposeAsync();

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            ReplayResult result = JournalReplayer.Replay(journal);
            Assert.True(result.Identical,
                "divergence: " + result.Divergence?.Recorded + " vs " + result.Divergence?.Replayed);
        }
    }
}
