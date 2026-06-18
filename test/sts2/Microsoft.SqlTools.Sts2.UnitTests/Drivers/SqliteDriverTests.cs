//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static async Task<List<ExecEvent>> ExecuteAsync(IDbSession session, string sql, int pageRows = 1000)
        {
            var events = new List<ExecEvent>();
            await foreach (ExecEvent execEvent in session.ExecuteAsync(
                new QueryExecuteRequest { QueryId = "q-1", Sql = sql, PageRows = pageRows }, CancellationToken.None))
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
        public async Task SyntaxErrorMapsToStableCode()
        {
            var driver = new SqliteDriver();
            await using IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);

            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(
                () => ExecuteAsync(session, "selct broken sql"));
            Assert.Equal("Sts2.QueryFailed.Server", ex.Code);
            Assert.NotNull(ex.Server);
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
            await ExecuteAsync(session, "with recursive c(n) as (select 1 union all select n+1 from c where n < 5) insert into t select n from c");

            // Cancel a (started but unconsumed) query — the per-query CTS must not poison later queries.
            await session.CancelAsync("q-cancelled", CancellationToken.None);

            // A subsequent query must run to completion, not be insta-cancelled by a sticky CTS.
            List<ExecEvent> events = await ExecuteAsync(session, "select n from t order by n");
            Assert.Equal(5, events.OfType<RowsPage>().Sum(p => p.Cells.Count));
            Assert.IsType<ExecCompleted>(events[^1]);
        }

        [Fact]
        public async Task SessionDisposeReleasesConnection()
        {
            var driver = new SqliteDriver();
            IDbSession session = await driver.OpenAsync(Request(":memory:"), CancellationToken.None);
            await session.DisposeAsync();
            await session.DisposeAsync(); // idempotent
        }
    }
}
