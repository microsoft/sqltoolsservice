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
using Microsoft.SqlTools.Sts2.Contracts;
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
        public async Task PageAndTimeoutOptionsReachTheDriverExecuteRequest() // QO-3
        {
            string connectionId = await session.OpenConnectionAsync();
            await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1","options":{"pageRows":128,"pageBytes":65536,"queryTimeoutMs":45000,"maxCellBytes":4096} }""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            Abstractions.QueryExecuteRequest? request = session.Driver.LastExecuteRequest;
            Assert.NotNull(request);
            Assert.Equal(128, request.PageRows);
            Assert.Equal(65536, request.PageBytes);
            Assert.Equal(45000, request.QueryTimeoutMs);
        }

        [Fact]
        public async Task AbsentOptionsReachTheDriverAsPinnedDefaults() // QO-3
        {
            string connectionId = await session.OpenConnectionAsync();
            await session.RequestAsync("v2/query.execute",
                $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            Abstractions.QueryExecuteRequest? request = session.Driver.LastExecuteRequest;
            Assert.NotNull(request);
            Assert.Equal(Sts2Defaults.PageRows, request.PageRows);
            Assert.Equal(Sts2Defaults.PageBytes, request.PageBytes);
            Assert.Equal(0, request.QueryTimeoutMs);
        }

        [Fact]
        public async Task ServerMessagePassesThroughVerbatimWithLine()
        {
            string connectionId = await session.OpenConnectionAsync();
            // Text with quote/backslash proves JSON-escaping-only passthrough (worksheet row 1:
            // verbatim, no rewording/truncation); line rides as a structured nullable field.
            const string verbatim = "Warning: Null value is eliminated by an aggregate or other SET operation. \"x\\y\"";
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "message", Text = verbatim, Number = 8153, Severity = 0, Line = 3 },
                    new FakeQueryStep { Type = "message", Text = "no line", Number = 0, Severity = 0 },
                    new FakeQueryStep { Type = "completed", RowsAffected = 0 },
                ],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"print"}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);

            List<OutboundRpcMessage> messages = await session.WaitForNotificationsAsync("v2/query.message", 2);
            JsonElement first = messages[0].Body!.Value;
            Assert.Equal(verbatim, first.GetProperty("text").GetString());
            Assert.Equal(8153, first.GetProperty("number").GetInt32());
            Assert.Equal(0, first.GetProperty("severity").GetInt32());
            Assert.Equal(3, first.GetProperty("line").GetInt32());
            Assert.Equal("info", first.GetProperty("messageClass").GetString());
            Assert.Equal(JsonValueKind.Null, messages[1].Body!.Value.GetProperty("line").ValueKind);
        }

        [Fact]
        public async Task CompleteCarriesCurrentDatabaseWhenDriverReportsIt()
        {
            string connectionId = await session.OpenConnectionAsync();
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps = [new FakeQueryStep { Type = "completed", RowsAffected = 0, Database = "OtherDb" }],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"use OtherDb"}""");
            List<OutboundRpcMessage> completes = await session.WaitForNotificationsAsync("v2/query.complete", 1);
            Assert.Equal("OtherDb", completes[0].Body!.Value.GetProperty("database").GetString());

            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps = [new FakeQueryStep { Type = "completed", RowsAffected = 1 }],
            });
            await session.RequestAsync("v2/query.execute", $$"""{"connectionId":"{{connectionId}}","sql":"select 1"}""");
            completes = await session.WaitForNotificationsAsync("v2/query.complete", 2);
            Assert.Equal(JsonValueKind.Null, completes[1].Body!.Value.GetProperty("database").ValueKind);
        }

        [Fact]
        public async Task WideCellsArriveBoundedWithTruncationHonesty() // STS2-3
        {
            string connectionId = await session.OpenConnectionAsync();
            // A huge NVARCHAR/XML-shaped cell with a 2-byte 'é' straddling the 4096-byte
            // bound, plus a 100000-byte blob page: both must arrive bounded and HONEST.
            string xml = "<blob>" + new string('a', 4089) + "é" + new string('b', 60000) + "</blob>";
            byte[] blob = new byte[100000];
            for (int i = 0; i < blob.Length; i++)
            {
                blob[i] = unchecked((byte)i);
            }
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 2 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellValue = xml },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellBytes = blob.Length, CellBinary = true },
                    new FakeQueryStep { Type = "completed", RowsAffected = 2 },
                ],
            });
            await session.RequestAsync("v2/query.execute",
                $$$"""{"connectionId":"{{{connectionId}}}","sql":"select wide","options":{"maxCellBytes":4096}}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 2);

            // String cell: wrapper with full-value bytes+digest; the prefix respects the
            // bound AND the 'é' code point ("<blob>" is 6 bytes, so 4095 fit, not 4096).
            JsonElement cell = rows[0].Body!.Value.GetProperty("rows")[0][1];
            Assert.Equal("truncated", cell.GetProperty("$t").GetString());
            Assert.Equal("string", cell.GetProperty("of").GetString());
            Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(xml), cell.GetProperty("bytes").GetInt32());
            Assert.Equal("sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(xml))),
                cell.GetProperty("digest").GetString());
            string prefix = cell.GetProperty("v").GetString()!;
            Assert.Equal("<blob>" + new string('a', 4089), prefix); // 4095 bytes: never split 'é'
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(prefix) <= 4096);
            Assert.StartsWith(prefix, xml, StringComparison.Ordinal);

            // Binary cell: bounded raw prefix (base64 on the wire), honest full-size metadata.
            JsonElement binaryCell = rows[1].Body!.Value.GetProperty("rows")[0][1];
            Assert.Equal("truncated", binaryCell.GetProperty("$t").GetString());
            Assert.Equal("binary", binaryCell.GetProperty("of").GetString());
            Assert.Equal(blob.Length, binaryCell.GetProperty("bytes").GetInt32());
            Assert.Equal(blob.AsSpan(0, 4096).ToArray(), Convert.FromBase64String(binaryCell.GetProperty("v").GetString()!));

            // The page payloads themselves are bounded (no 100KB frames slipped through).
            Assert.All(rows, page => Assert.True(page.Body!.Value.GetRawText().Length < 3 * 4096,
                "rows page exceeded the bounded-payload expectation"));
        }

        [Fact]
        public async Task ExactBoundCellPassesThroughAndOneOverTruncates() // STS2-3 boundary
        {
            string connectionId = await session.OpenConnectionAsync();
            string atBound = new('x', 64);
            string oneOver = new('x', 65);
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 2 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellValue = atBound },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellValue = oneOver },
                    new FakeQueryStep { Type = "completed", RowsAffected = 2 },
                ],
            });
            await session.RequestAsync("v2/query.execute",
                $$$"""{"connectionId":"{{{connectionId}}}","sql":"select edge","options":{"maxCellBytes":64}}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 2);

            JsonElement exact = rows[0].Body!.Value.GetProperty("rows")[0][1];
            Assert.Equal(JsonValueKind.String, exact.ValueKind); // exactly at bound: untouched
            Assert.Equal(atBound, exact.GetString());

            JsonElement over = rows[1].Body!.Value.GetProperty("rows")[0][1];
            Assert.Equal("truncated", over.GetProperty("$t").GetString());
            Assert.Equal(65, over.GetProperty("bytes").GetInt32());
            Assert.Equal(new string('x', 64), over.GetProperty("v").GetString());
        }

        [Fact]
        public async Task AbsentOrZeroMaxCellBytesKeepsTodaysBehavior() // STS2-3
        {
            string connectionId = await session.OpenConnectionAsync();
            // 100KB is far below the pinned 1 MiB service default: with no client bound
            // (absent options, then an explicit 0) the cell arrives whole, untouched.
            string wide = new('w', 100_000);
            string[] optionVariants = ["", ""","options":{"maxCellBytes":0}"""];
            for (int i = 0; i < optionVariants.Length; i++)
            {
                session.Driver.EnqueueQuery(new FakeQueryScript
                {
                    Steps =
                    [
                        new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 2 },
                        new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellValue = wide },
                        new FakeQueryStep { Type = "completed", RowsAffected = 1 },
                    ],
                });
                OutboundRpcMessage execute = await session.RequestAsync("v2/query.execute",
                    $$"""{"connectionId":"{{connectionId}}","sql":"select wide"{{optionVariants[i]}}}""");
                Assert.Equal("rpc.out.result", execute.Kind);
                await session.WaitForNotificationsAsync("v2/query.complete", i + 1); // one active query per connection
            }
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 2);
            Assert.All(rows, page =>
            {
                JsonElement cell = page.Body!.Value.GetProperty("rows")[0][1];
                Assert.Equal(JsonValueKind.String, cell.ValueKind);
                Assert.Equal(wide, cell.GetString());
            });
        }

        [Fact]
        public async Task OversizedMaxCellBytesRequestClampsToTheServiceLimit() // STS2-3
        {
            string connectionId = await session.OpenConnectionAsync();
            // The client asks for 8 MiB; the service limit stays 1 MiB, so a cell one page
            // over the pinned default still truncates (with the pinned 64KB retained prefix).
            string huge = new('h', Sts2Defaults.MaxCellBytes + 8);
            session.Driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 2 },
                    new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 1, CellValue = huge },
                    new FakeQueryStep { Type = "completed", RowsAffected = 1 },
                ],
            });
            await session.RequestAsync("v2/query.execute",
                $$$"""{"connectionId":"{{{connectionId}}}","sql":"select huge","options":{"maxCellBytes":8388608}}""");
            await session.WaitForNotificationsAsync("v2/query.complete", 1);
            List<OutboundRpcMessage> rows = await session.WaitForNotificationsAsync("v2/query.rows", 1);

            JsonElement cell = rows[0].Body!.Value.GetProperty("rows")[0][1];
            Assert.Equal("truncated", cell.GetProperty("$t").GetString());
            Assert.Equal(huge.Length, cell.GetProperty("bytes").GetInt32());
            Assert.Equal(Sts2Defaults.TruncatedPrefixBytes,
                System.Text.Encoding.UTF8.GetByteCount(cell.GetProperty("v").GetString()!));
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
