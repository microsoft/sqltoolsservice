//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.Sqlite;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>SPEC §10.3 / M4: the Sqlite adapter against real in-memory and file I/O.</summary>
    public class SqliteDriverTests
    {
        private static ConnectionOpenRequest Request(string server) => new()
        {
            Server = server,
            Auth = new SecretMaterial { Kind = "integrated" },
        };

        private static async Task<List<ExecEvent>> ExecuteAsync(
            IDbSession session,
            string sql,
            int pageRows = 1000,
            int pageBytes = 0,
            int queryTimeoutMs = 0,
            int maxCellBytes = 0)
        {
            var events = new List<ExecEvent>();
            await foreach (ExecEvent execEvent in session.ExecuteAsync(
                new QueryExecuteRequest
                {
                    QueryId = "q-1",
                    Sql = sql,
                    PageRows = pageRows,
                    PageBytes = pageBytes,
                    QueryTimeoutMs = queryTimeoutMs,
                    MaxCellBytes = maxCellBytes,
                },
                CancellationToken.None))
            {
                events.Add(execEvent);
            }
            return events;
        }

        [Fact]
        public async Task InMemoryOpenReportsServerInfo()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            Assert.Equal("SQLite", session.Server.Product);
            Assert.Equal("sqlite", session.Server.Dialect);
            Assert.False(string.IsNullOrEmpty(session.Server.Version));
        }

        [Fact]
        public async Task ExecutesAndStreamsRealRows()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            await ExecuteAsync(session, "create table t(id integer, name text, score real, data blob, maybe text)");
            await ExecuteAsync(session, "insert into t values (1,'a',1.5,x'01020304',null),(2,'b',2.5,x'aa',null)");

            List<ExecEvent> events = await ExecuteAsync(session, "select id, name, score, data, maybe from t order by id");

            Assert.IsType<ExecStarted>(events[0]);
            ResultSetStarted resultSet = Assert.IsType<ResultSetStarted>(events[1]);
            Assert.Equal(5, resultSet.Columns.Count);
            Assert.Equal("id", resultSet.Columns[0].Name);

            RowsPage page = Assert.IsType<RowsPage>(events[2]);
            Assert.Equal(2, page.Cells.Count);
            // The port returns plain CLR values; the runner does wire encoding (§7.7).
            Assert.Equal(1, Convert.ToInt64(page.Cells[0][0]));      // INTEGER -> long
            Assert.Equal("a", page.Cells[0][1]);                    // TEXT -> string
            Assert.Equal(1.5, Convert.ToDouble(page.Cells[0][2]));  // REAL -> double
            Assert.Equal([1, 2, 3, 4], (byte[])page.Cells[0][3]!);  // BLOB -> byte[]
            Assert.Null(page.Cells[0][4]);                          // NULL -> null

            Assert.IsType<ResultSetCompleted>(events[^2]);
            Assert.IsType<ExecCompleted>(events[^1]);
        }

        [Fact]
        public async Task MultiStatementBatchPreservesQuotedSemicolonsAndTriggerBodies()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            List<ExecEvent> batchEvents = await ExecuteAsync(session, """
                create table source(v text);
                create table audit(v text);
                create trigger source_ai after insert on source begin
                    insert into audit values ('literal;semicolon');
                    insert into audit values (new.v);
                end;
                insert into source values ('input;value');
                select 'first;result';
                select count(*) from audit;
                """);
            Assert.Equal([1L], Assert.IsType<ExecCompleted>(batchEvents[^1]).RowsAffected);
            Assert.Equal(
                [0, 1],
                batchEvents.OfType<ResultSetStarted>().Select(result => result.ResultSetId));
            Assert.Equal(
                ["first;result", 2L],
                batchEvents.OfType<RowsPage>().Select(page => page.Cells[0][0]));

            RowsPage page = Assert.Single(
                (await ExecuteAsync(session, "select v from audit order by rowid")).OfType<RowsPage>());
            Assert.Equal("literal;semicolon", page.Cells[0][0]);
            Assert.Equal("input;value", page.Cells[1][0]);
        }

        [Fact]
        public async Task OversizedTextAndBlobCellsAreStreamedIntoBoundedTruncationValues()
        {
            const int MaxCellBytes = 1024;
            const int TextLength = 200_000;
            const int MultibyteCharacters = 700;
            const int BlobLength = 5_000_000;
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            RowsPage page = Assert.Single((await ExecuteAsync(
                session,
                $"select printf('%.*c', {TextLength}, 'x'), " +
                $"printf('%.*c', {MultibyteCharacters}, 'é'), zeroblob({BlobLength})",
                maxCellBytes: MaxCellBytes)).OfType<RowsPage>());
            IReadOnlyList<object?> row = Assert.Single(page.Cells);

            DriverTruncatedValue text = Assert.IsType<DriverTruncatedValue>(row[0]);
            Assert.Equal("string", text.Kind);
            Assert.Equal(new string('x', MaxCellBytes), text.PrefixText);
            Assert.Equal(TextLength, text.TotalBytes);
            Assert.Equal(HashUtf8(new string('x', TextLength)), text.DigestHex);

            DriverTruncatedValue multibyte = Assert.IsType<DriverTruncatedValue>(row[1]);
            Assert.Equal("string", multibyte.Kind);
            Assert.Equal(new string('é', MaxCellBytes / 2), multibyte.PrefixText);
            Assert.Equal(MultibyteCharacters * 2L, multibyte.TotalBytes);
            Assert.Equal(HashUtf8(new string('é', MultibyteCharacters)), multibyte.DigestHex);

            DriverTruncatedValue blob = Assert.IsType<DriverTruncatedValue>(row[2]);
            Assert.Equal("binary", blob.Kind);
            Assert.Equal(MaxCellBytes, Assert.IsType<byte[]>(blob.PrefixBytes).Length);
            Assert.All(blob.PrefixBytes!, value => Assert.Equal(0, value));
            Assert.Equal(BlobLength, blob.TotalBytes);
            Assert.Equal(HashRepeatedByte(0, BlobLength), blob.DigestHex);
        }

        [Fact]
        public async Task ResultMetadataUsesProviderNullability()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table metadata(required text not null, optional text)");

            ResultSetStarted resultSet = Assert.Single(
                (await ExecuteAsync(session, "select required, optional from metadata")).OfType<ResultSetStarted>());

            Assert.False(resultSet.Columns[0].Nullable);
            Assert.True(resultSet.Columns[1].Nullable);
        }

        [Fact]
        public async Task RowIdPrimaryKeyNullabilityDoesNotOverstateTextConstraint()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table text_key(id text primary key, payload text)");
            await ExecuteAsync(session, "insert into text_key values(null, 'accepted')");

            List<ExecEvent> textEvents = await ExecuteAsync(
                session,
                "select id, payload from text_key");
            ResultSetStarted textResult = Assert.Single(textEvents.OfType<ResultSetStarted>());
            Assert.Null(textResult.Columns[0].Nullable);
            Assert.True(textResult.Columns[1].Nullable);
            Assert.Null(Assert.Single(Assert.Single(textEvents.OfType<RowsPage>()).Cells)[0]);

            await ExecuteAsync(session, "create table integer_key(id integer primary key)");
            ResultSetStarted integerResult = Assert.Single(
                (await ExecuteAsync(session, "select id from integer_key")).OfType<ResultSetStarted>());
            Assert.False(integerResult.Columns[0].Nullable);
        }

        [Fact]
        public async Task PagingSplitsRowsByPageRows()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table nums(n integer)");
            // 10 rows via a recursive CTE insert.
            await ExecuteAsync(session, "with recursive c(n) as (select 1 union all select n+1 from c where n < 10) insert into nums select n from c");

            List<ExecEvent> events = await ExecuteAsync(session, "select n from nums order by n", pageRows: 3);
            List<RowsPage> pages = events.OfType<RowsPage>().ToList();
            Assert.Equal(4, pages.Count); // 3+3+3+1
            Assert.Equal([0, 1, 2, 3], pages.Select(p => p.PageSeq));
            Assert.Equal([0L, 3L, 6L, 9L], pages.Select(p => p.RowOffset));
            Assert.Single(pages[^1].Cells);
        }

        [Fact]
        public async Task PagingSplitsRowsByPageBytes()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            string wide = new('\u00e9', 100);
            await ExecuteAsync(session, "create table texts(v text)");
            await ExecuteAsync(
                session,
                "insert into texts values ('" + wide + "'),('" + wide + "'),('" + wide + "'),('" + wide + "')");

            List<ExecEvent> events = await ExecuteAsync(
                session,
                "select v from texts",
                pageRows: 1000,
                pageBytes: 800);
            List<RowsPage> pages = events.OfType<RowsPage>().ToList();

            Assert.Equal(4, pages.Count);
            Assert.All(pages, page => Assert.Single(page.Cells));
            Assert.Equal([0L, 1L, 2L, 3L], pages.Select(page => page.RowOffset));
        }

        [Fact]
        public async Task BlobPagingIncludesTypedBinaryWrapperBytes()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table blobs(v blob)");
            await ExecuteAsync(
                session,
                "insert into blobs values (zeroblob(300)),(zeroblob(300)),(zeroblob(300))");

            List<RowsPage> pages = (await ExecuteAsync(
                session,
                "select v from blobs",
                pageRows: 1000,
                pageBytes: 830)).OfType<RowsPage>().ToList();

            Assert.Equal(3, pages.Count);
            Assert.All(pages, page => Assert.Single(page.Cells));
        }

        [Fact]
        public async Task NonFiniteDoublePagingIncludesTypedWrapperBytes()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            List<RowsPage> pages = (await ExecuteAsync(
                session,
                "select 1e999 union all select 1e999 union all select 1e999",
                pageRows: 1000,
                pageBytes: 70)).OfType<RowsPage>().ToList();

            Assert.Equal(3, pages.Count);
            Assert.All(pages, page => Assert.Single(page.Cells));
            Assert.All(pages, page => Assert.True(double.IsPositiveInfinity(Assert.IsType<double>(page.Cells[0][0]))));
        }

        [Fact]
        public async Task QueryTimeoutInterruptsLongRunningCommand()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            const string ExpensiveSql = """
                with recursive a(x) as (select 1 union all select x + 1 from a where x < 1000)
                select sum(a.x * b.x * c.x) from a cross join a b cross join a c
                """;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await Task.Run(() => ExecuteAsync(session, ExpensiveSql, queryTimeoutMs: 100))
                    .WaitAsync(TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task SyntaxErrorMapsToStableCode()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(
                () => ExecuteAsync(session, "selct broken sql"));
            Assert.Equal("Sts2.QueryFailed.Server", ex.Code);
            Assert.NotNull(ex.Server);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task OpenFailureDoesNotExposeProviderException()
        {
            string database = Path.Combine(
                Path.GetTempPath(),
                "sts2-missing-" + Guid.NewGuid().ToString("N"),
                "database.sqlite");
            var driver = new SqliteDriver();

            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(
                () => driver.OpenAsync(Request(database), CancellationToken.None).AsTask());

            Assert.NotNull(ex.Server);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task UnexpectedOpenFailureUsesStableDriverException()
        {
            var driver = new SqliteDriver();

            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(
                () => driver.OpenAsync(Request("\0"), CancellationToken.None).AsTask());

            Assert.Equal("Sts2.ConnectionFailed.Network", ex.Code);
            Assert.Null(ex.InnerException);
        }

        [Theory]
        [InlineData("mode")]
        [InlineData("cache")]
        public async Task InvalidConnectionOptionIsRejected(string option)
        {
            ConnectionOpenRequest request = Request(":memory:") with
            {
                Options = new Dictionary<string, string> { [option] = "not-a-real-value" },
            };

            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(
                () => new SqliteDriver().OpenAsync(request, CancellationToken.None).AsTask());

            Assert.Equal("Sts2.InvalidRequest", ex.Code);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task FileBackedRoundTripsAcrossSessions()
        {
            string path = Path.Combine(Path.GetTempPath(), "sts2-sqlite-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                var driver = new SqliteDriver();
                await using (IDbSession writer = await driver.OpenAsync(Request(path), CancellationToken.None))
                {
                    await ExecuteAsync(writer, "create table persisted(v text)");
                    await ExecuteAsync(writer, "insert into persisted values ('survives')");
                }

                await using IDbSession reader = await driver.OpenAsync(Request(path), CancellationToken.None);
                List<ExecEvent> events = await ExecuteAsync(reader, "select v from persisted");
                RowsPage page = Assert.Single(events.OfType<RowsPage>());
                Assert.Equal("survives", Convert.ToString(page.Cells[0][0]));
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }

        [Fact]
        public async Task CancelDoesNotStickToTheNextQuery() // R016
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table t(n integer)");
            await ExecuteAsync(session, "insert into t values (1),(2),(3),(4),(5)");

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task ConsumeLongQueryAsync()
            {
                await foreach (ExecEvent execEvent in session.ExecuteAsync(
                    new QueryExecuteRequest
                    {
                        QueryId = "q-cancelled",
                        Sql = "with recursive a(x) as (select 1 union all select x + 1 from a where x < 1000) select sum(a.x * b.x * c.x) from a cross join a b cross join a c",
                    },
                    CancellationToken.None))
                {
                    if (execEvent is ExecStarted)
                    {
                        started.TrySetResult();
                    }
                }
            }

            Task activeQuery = Task.Run(ConsumeLongQueryAsync);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100); // let the provider enter sqlite3_step
            await session.CancelAsync("q-from-an-older-query", CancellationToken.None);
            await Task.Delay(100);
            Assert.False(activeQuery.IsCompleted);
            await session.CancelAsync("q-cancelled", CancellationToken.None);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await activeQuery.WaitAsync(TimeSpan.FromSeconds(5)));

            // A subsequent query must run to completion, not be insta-cancelled by a sticky CTS.
            List<ExecEvent> events = await ExecuteAsync(session, "select n from t order by n");
            Assert.Equal(5, events.OfType<RowsPage>().Sum(p => p.Cells.Count));
            Assert.IsType<ExecCompleted>(events[^1]);
        }

        [Fact]
        public async Task CancelDoesNotExecuteTrailingBatchMutation()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await ExecuteAsync(session, "create table side_effect(v integer)");
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task ConsumeBatchAsync()
            {
                await foreach (ExecEvent execEvent in session.ExecuteAsync(
                    new QueryExecuteRequest
                    {
                        QueryId = "q-cancel-batch",
                        Sql = """
                            with recursive a(x) as (select 1 union all select x + 1 from a where x < 1000)
                            select sum(a.x * b.x * c.x) from a cross join a b cross join a c;
                            insert into side_effect values (1);
                            """,
                    },
                    CancellationToken.None))
                {
                    if (execEvent is ExecStarted)
                    {
                        started.TrySetResult();
                    }
                }
            }

            Task activeQuery = Task.Run(ConsumeBatchAsync);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100); // let the provider enter sqlite3_step
            await session.CancelAsync("q-cancel-batch", CancellationToken.None);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await activeQuery.WaitAsync(TimeSpan.FromSeconds(5)));

            RowsPage countPage = Assert.Single(
                (await ExecuteAsync(session, "select count(*) from side_effect")).OfType<RowsPage>());
            Assert.Equal(0L, countPage.Cells[0][0]);
            Assert.IsType<ExecCompleted>((await ExecuteAsync(session, "select 1"))[^1]);
        }

        [Fact]
        public async Task DisposingAfterExecStartedClearsPublishedQueryState()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            FieldInfo currentQueryCancel = session.GetType().GetField(
                "currentQueryCancel",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            FieldInfo currentQueryId = session.GetType().GetField(
                "currentQueryId",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            IAsyncEnumerator<ExecEvent> enumerator = session.ExecuteAsync(
                new QueryExecuteRequest { QueryId = "q-disposed", Sql = "select 1" },
                CancellationToken.None).GetAsyncEnumerator();

            Assert.True(await enumerator.MoveNextAsync());
            Assert.IsType<ExecStarted>(enumerator.Current);
            Assert.NotNull(currentQueryCancel.GetValue(session));
            Assert.Equal("q-disposed", currentQueryId.GetValue(session));
            await enumerator.DisposeAsync();

            Assert.Null(currentQueryCancel.GetValue(session));
            Assert.Null(currentQueryId.GetValue(session));
            List<ExecEvent> events = await ExecuteAsync(session, "select 2");
            Assert.IsType<ExecCompleted>(events[^1]);
        }

        [Fact]
        public async Task CompletionIsPublishedAfterProviderCleanup()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            FieldInfo currentQueryCancel = session.GetType().GetField(
                "currentQueryCancel",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            await using IAsyncEnumerator<ExecEvent> enumerator = session.ExecuteAsync(
                new QueryExecuteRequest { QueryId = "q-first", Sql = "select 1" },
                CancellationToken.None).GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync() && enumerator.Current is not ExecCompleted)
            {
            }

            Assert.IsType<ExecCompleted>(enumerator.Current);
            Assert.Null(currentQueryCancel.GetValue(session));
            Assert.IsType<ExecCompleted>((await ExecuteAsync(session, "select 2"))[^1]);
        }

        [Fact]
        public async Task SessionDisposeReleasesConnection()
        {
            var driver = new SqliteDriver();
            IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await session.DisposeAsync();
            await session.DisposeAsync(); // idempotent
        }

        private static string HashUtf8(string value) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static string HashRepeatedByte(byte value, int count)
        {
            byte[] chunk = new byte[Math.Min(32768, count)];
            if (value != 0)
            {
                Array.Fill(chunk, value);
            }
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            int remaining = count;
            while (remaining > 0)
            {
                int length = Math.Min(chunk.Length, remaining);
                hash.AppendData(chunk, 0, length);
                remaining -= length;
            }
            return Convert.ToHexStringLower(hash.GetHashAndReset());
        }
    }
}
