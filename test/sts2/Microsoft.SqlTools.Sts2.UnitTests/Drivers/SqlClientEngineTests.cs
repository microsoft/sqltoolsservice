//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// SPEC §14.5 dialect:tsql engine tests against a real SQL Server. These SKIP (not
    /// fail) when no server is reachable (STS2_SQLSERVER_CONNSTRING unset/unreachable);
    /// CI/nightly sets the variable and runs them. Tagged Category=Engine so the local
    /// quick gate never selects them.
    /// </summary>
    [Trait("Category", "Engine")]
    public sealed class SqlClientEngineTests
    {
        private readonly ITestOutputHelper output;

        public SqlClientEngineTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static ConnectionOpenRequest OpenRequest()
        {
            var builder = new SqlConnectionStringBuilder(SqlServerProbe.ConnectionString);
            var options = new Dictionary<string, string>();
            if (builder.Encrypt == SqlConnectionEncryptOption.Strict)
            {
                options["encrypt"] = "strict";
            }
            if (builder.TrustServerCertificate)
            {
                options["trustServerCertificate"] = "true";
            }
            return new ConnectionOpenRequest
            {
                Server = builder.DataSource,
                Database = string.IsNullOrEmpty(builder.InitialCatalog) ? "master" : builder.InitialCatalog,
                Auth = builder.IntegratedSecurity
                    ? new SecretMaterial { Kind = "integrated" }
                    : new SecretMaterial { Kind = "sqlLogin", User = builder.UserID, Secret = builder.Password },
                ConnectTimeoutMs = 15000,
                Options = options,
            };
        }

        private static async Task<List<ExecEvent>> ExecuteAsync(IDbSession session, string sql)
        {
            var events = new List<ExecEvent>();
            await foreach (ExecEvent execEvent in session.ExecuteAsync(
                new QueryExecuteRequest { QueryId = "q-1", Sql = sql, PageRows = 1000 }, CancellationToken.None))
            {
                events.Add(execEvent);
            }
            return events;
        }

        [Fact]
        public async Task OpensAndReportsTsqlServerInfo()
        {
            if (!EngineGate.ShouldRun(output))
            {
                return;
            }
            var driver = new SqlClientDriver();
            await using IDbSession session = await driver.OpenAsync(OpenRequest(), CancellationToken.None);
            Assert.Equal("Microsoft SQL Server", session.Server.Product);
            Assert.Equal("tsql", session.Server.Dialect);
        }

        [Fact]
        public async Task TypeEncodingMatrixOverRealEngine()
        {
            if (!EngineGate.ShouldRun(output))
            {
                return;
            }
            var driver = new SqlClientDriver();
            await using IDbSession session = await driver.OpenAsync(OpenRequest(), CancellationToken.None);

            List<ExecEvent> events = await ExecuteAsync(session, """
                select
                    cast(12.50 as decimal(10,2)) as dec,
                    cast('2026-06-13T01:02:03.1234567+05:00' as datetimeoffset) as dto,
                    cast('2026-06-13' as date) as d,
                    cast(0x01020304 as varbinary(8)) as bin,
                    cast('00000000-0000-0000-0000-000000000001' as uniqueidentifier) as g,
                    cast(null as int) as nul
                """);
            RowsPage page = Assert.Single(events.OfType<RowsPage>());
            IReadOnlyList<object?> row = page.Cells[0];
            Assert.IsType<decimal>(row[0]);
            Assert.IsType<DateTimeOffset>(row[1]);
            Assert.IsType<DateTime>(row[2]);
            Assert.IsType<byte[]>(row[3]);
            Assert.IsType<Guid>(row[4]);
            Assert.Null(row[5]);
        }

        [Fact]
        public async Task SyntaxErrorMapsToStableServerCode()
        {
            if (!EngineGate.ShouldRun(output))
            {
                return;
            }
            var driver = new SqlClientDriver();
            await using IDbSession session = await driver.OpenAsync(OpenRequest(), CancellationToken.None);
            DbDriverException ex = await Assert.ThrowsAsync<DbDriverException>(() => ExecuteAsync(session, "selct broken"));
            Assert.Equal("Sts2.QueryFailed.Server", ex.Code);
            Assert.NotNull(ex.Server);
        }
    }
}
