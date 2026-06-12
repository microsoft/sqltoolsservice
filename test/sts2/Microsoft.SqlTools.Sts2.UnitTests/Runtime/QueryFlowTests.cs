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
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Replay;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    /// <summary>SPEC §16 M3: query streaming end-to-end on FakeDriver.</summary>
    public sealed class QueryFlowTests : IAsyncDisposable, IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-query-test-" + Guid.NewGuid().ToString("N"));
        private readonly Sts2TestSession session;

        public QueryFlowTests()
        {
            session = new Sts2TestSession(directory, "query-test");
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
        public async Task HappyPathStreamsResultSetRowsAndCompletes()
        {
            string connectionId = await session.OpenConnectionAsync();
            OutboundRpcMessage execute = await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            Assert.Equal("rpc.out.result", execute.Kind);
            string queryId = execute.Body!.Value.GetProperty("queryId").GetString()!;

            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("succeeded", completes[0].Body!.Value.GetProperty("status").GetString());
            Assert.Equal(queryId, completes[0].Body!.Value.GetProperty("queryId").GetString());

            List<OutboundRpcMessage> resultSets = await session.WaitForNotificationsAsync("v2/query.resultSet", 1);
            Assert.Equal(2, resultSets[0].Body!.Value.GetProperty("columns").GetArrayLength());
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 1);
            Assert.Equal(3, rows[0].Body!.Value.GetProperty("rows").GetArrayLength());
            Assert.Equal(0, rows[0].Body!.Value.GetProperty("pageSeq").GetInt32());
        }

        [Fact]
        public async Task ExactlyOneCompletePerQueryAndOrderingHolds()
        {
            string connectionId = await session.OpenConnectionAsync();
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 1 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 2 },
                    new FakeQueryStep { Type = "message", Text = "hi", Number = 50000, Severity = 0 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 2 },
                    new FakeQueryStep { Type = "resultSetDone", ResultSetId = 0, RowCount = 4 },
                    new FakeQueryStep { Type = "completed", RowsAffected = 4 },
                ],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select x"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            // I2: exactly one complete. Ordering: resultSet before rows; no output after complete.
            List<OutboundRpcMessage> notifications = session.Emitted.Where(m => m.Kind == "rpc.out.notify").ToList();
            Assert.Equal(1, notifications.Count(n => n.Type == "v2/query.complete"));
            int resultSetIndex = notifications.FindIndex(n => n.Type == "v2/query.resultSet");
            int firstRowsIndex = notifications.FindIndex(n => n.Type == "v2/query.rows");
            int completeIndex = notifications.FindIndex(n => n.Type == "v2/query.complete");
            Assert.True(resultSetIndex < firstRowsIndex, "resultSet must precede rows (I-ordering)");
            Assert.Equal(notifications.Count - 1, completeIndex); // complete is last (I3)

            // pageSeq gapless, rowOffset monotonic (SPEC §7.5).
            List<JsonElement> pages = notifications.Where(n => n.Type == "v2/query.rows").Select(n => n.Body!.Value).ToList();
            Assert.Equal([0, 1], pages.Select(p => p.GetProperty("pageSeq").GetInt32()));
            Assert.Equal([0L, 2L], pages.Select(p => p.GetProperty("rowOffset").GetInt64()));
        }

        [Fact]
        public async Task BackpressureStopsAtWindowUntilAcked()
        {
            string connectionId = await session.OpenConnectionAsync();
            // 8 pages scripted; window is 4 — without acks only 4 may arrive (I9).
            var steps = new List<FakeQueryStep> { new() { Type = "resultSet", ResultSetId = 0, Columns = 1 } };
            steps.AddRange(Enumerable.Range(0, 8).Select(_ => new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1 }));
            steps.Add(new FakeQueryStep { Type = "completed", RowsAffected = 8 });
            session.Driver.EnqueueQuery(new FakeQueryScript { Steps = steps });

            OutboundRpcMessage execute = await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select big"}""");
            string queryId = execute.Body!.Value.GetProperty("queryId").GetString()!;

            await session.WaitForNotificationsAsync("v2/query.rows", 4);
            await Task.Delay(200); // give a 5th page every chance to arrive illegally
            Assert.Equal(4, session.Emitted.Count(m => m.Type == "v2/query.rows"));
            Assert.Empty(session.Emitted.Where(m => m.Type == "v2/query.complete"));

            // High-water ack opens the window; the stream finishes.
            await session.NotifyAsync("v2/query.ack", $$"""{"queryId":"{{queryId}}","throughPageSeq":3}""");
            await session.WaitForNotificationsAsync("v2/query.rows", 8);
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
        }

        [Fact]
        public async Task CancelMidStreamCompletesWithCanceledStatus()
        {
            string connectionId = await session.OpenConnectionAsync();
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 1 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 5 },
                    new FakeQueryStep { Type = "hang" },
                ],
            });
            OutboundRpcMessage execute = await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select forever"}""");
            string queryId = execute.Body!.Value.GetProperty("queryId").GetString()!;
            await session.WaitForNotificationsAsync("v2/query.rows", 1);

            OutboundRpcMessage cancel = await session.RequestAsync("v2/query.cancel", $$"""{"queryId":"{{queryId}}"}""");
            Assert.Equal("rpc.out.result", cancel.Kind);

            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("canceled", completes[0].Body!.Value.GetProperty("status").GetString());

            // Second cancel and a dispose are idempotent.
            Assert.Equal("rpc.out.result", (await session.RequestAsync("v2/query.cancel", $$"""{"queryId":"{{queryId}}"}""")).Kind);
            Assert.Equal("rpc.out.result", (await session.RequestAsync("v2/query.dispose", $$"""{"queryId":"{{queryId}}"}""")).Kind);
        }

        [Fact]
        public async Task ServerErrorMidStreamCompletesWithErrorStatus()
        {
            string connectionId = await session.OpenConnectionAsync();
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 1 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1 },
                    new FakeQueryStep { Type = "error", ErrorCode = "Sts2.QueryFailed.Server", Text = "Divide by zero.", Number = 8134, Severity = 16 },
                ],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1/0"}""");

            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            JsonElement body = completes[0].Body!.Value;
            Assert.Equal("error", body.GetProperty("status").GetString());
            Assert.Equal("Sts2.QueryFailed.Server", body.GetProperty("error").GetProperty("code").GetString());
            Assert.Equal(8134, body.GetProperty("error").GetProperty("server").GetProperty("number").GetInt32());
        }

        [Fact]
        public async Task CloseWhileQueryActiveCancelsThenCloses()
        {
            string connectionId = await session.OpenConnectionAsync();
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 1 },
                    new FakeQueryStep { Type = "hang" },
                ],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select forever"}""");
            await session.WaitForNotificationsAsync("v2/query.resultSet", 1);

            OutboundRpcMessage close = await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            Assert.Equal("rpc.out.result", close.Kind);

            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("canceled", completes[0].Body!.Value.GetProperty("status").GetString());
            Assert.Equal(0, session.Driver.OpenSessionCount); // I8
        }

        [Fact]
        public async Task QueryOnUnknownConnectionIsNotFound()
        {
            OutboundRpcMessage error = await session.RequestAsync("v2/query.execute",
                """{"connectionId":"c-nope","sql":"select 1"}""");
            Assert.Equal("rpc.out.error", error.Kind);
            Assert.Equal("Sts2.NotFound", error.Body!.Value.GetProperty("data").GetProperty("code").GetString());
        }

        [Fact]
        public async Task QuerySessionReplaysIdentically()
        {
            string connectionId = await session.OpenConnectionAsync();
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
            await session.RequestAsync("v2/connection.close", $$"""{"connectionId":"{{connectionId}}"}""");
            await session.DisposeAsync();

            ReplayResult replay = JournalReplayer.Replay(JournalReader.ReadAll(directory));
            Assert.True(replay.Identical,
                "divergence: " + replay.Divergence?.Recorded + " vs " + replay.Divergence?.Replayed);
            Assert.DoesNotContain(replay.FinalState.Queries.Values, q => q.Phase == "running");
        }
    }
}
