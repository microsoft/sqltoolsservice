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

        private static async Task<List<ExecEvent>> ExecuteAsync(IDbSession session, string sql, bool vectorBinary = false)
        {
            var events = new List<ExecEvent>();
            await foreach (ExecEvent execEvent in session.ExecuteAsync(
                new QueryExecuteRequest { QueryId = "q-1", Sql = sql, PageRows = 1000, VectorBinary = vectorBinary }, CancellationToken.None))
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
        public async Task VectorReadsAsJsonTextByDefaultAndTypedWhenNegotiated() // D-0018/D-0019
        {
            if (!EngineGate.ShouldRun(output))
            {
                return;
            }
            var driver = new SqlClientDriver();
            await using IDbSession session = await driver.OpenAsync(OpenRequest(), CancellationToken.None);
            const string Sql = "select cast('[1.5,-2.5,3.25]' as vector(3)) as v";

            // Default (no opt-in): the JSON-array text, full precision, as an
            // ordinary string cell — never a provider type name.
            List<ExecEvent> textEvents = await ExecuteAsync(session, Sql);
            RowsPage textPage = Assert.Single(textEvents.OfType<RowsPage>());
            string text = Assert.IsType<string>(textPage.Cells[0][0]);
            float[] parsed = System.Text.Json.JsonSerializer.Deserialize<float[]>(text)!;
            Assert.Equal([1.5f, -2.5f, 3.25f], parsed);

            // Vector column metadata: length = 8 + 4 * dimensions.
            ResultSetStarted resultSet = Assert.Single(textEvents.OfType<ResultSetStarted>());
            Assert.Equal("vector", resultSet.Columns[0].EngineType.ToLowerInvariant());
            Assert.Equal(20, resultSet.Columns[0].Length);

            // Negotiated: typed little-endian component bytes, exact.
            List<ExecEvent> typedEvents = await ExecuteAsync(session, Sql, vectorBinary: true);
            RowsPage typedPage = Assert.Single(typedEvents.OfType<RowsPage>());
            DriverVectorValue vector = Assert.IsType<DriverVectorValue>(typedPage.Cells[0][0]);
            Assert.Equal(3, vector.Dimensions);
            Assert.Equal("float32", vector.BaseType);
            Assert.Equal("f32le", vector.Encoding);
            Assert.Equal(12, vector.ComponentBytes.Length);
            float[] decoded = new float[3];
            for (int i = 0; i < decoded.Length; i++)
            {
                decoded[i] = BitConverter.Int32BitsToSingle(
                    System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                        vector.ComponentBytes.AsSpan(i * 4, 4)));
            }
            Assert.Equal([1.5f, -2.5f, 3.25f], decoded);

            // NULL vector stays an ordinary null cell either way.
            List<ExecEvent> nullEvents = await ExecuteAsync(session, "select cast(null as vector(3)) as v", vectorBinary: true);
            Assert.Null(Assert.Single(nullEvents.OfType<RowsPage>()).Cells[0][0]);
        }

        [Fact]
        public async Task ClrUdtColumnsTransportAsBinaryInsteadOfFailingTheQuery() // D-0018
        {
            if (!EngineGate.ShouldRun(output))
            {
                return;
            }
            var driver = new SqlClientDriver();
            await using IDbSession session = await driver.OpenAsync(OpenRequest(), CancellationToken.None);

            List<ExecEvent> events = await ExecuteAsync(session, """
                select
                    geometry::Point(1, 2, 0) as geom,
                    geography::Point(47.6, -122.3, 4326) as geog,
                    hierarchyid::GetRoot() as h
                """);
            RowsPage page = Assert.Single(events.OfType<RowsPage>());
            IReadOnlyList<object?> row = page.Cells[0];

            // geometry point: CLR serialization = SRID int32 LE + version 01 + ...
            byte[] geom = Assert.IsType<byte[]>(row[0]);
            Assert.Equal(22, geom.Length);
            Assert.Equal(0, BitConverter.ToInt32(geom, 0)); // SRID 0
            Assert.Equal(0x01, geom[4]);

            byte[] geog = Assert.IsType<byte[]>(row[1]);
            Assert.Equal(4326, BitConverter.ToInt32(geog, 0)); // SRID 4326

            Assert.IsType<byte[]>(row[2]); // hierarchyid root serializes (possibly empty)
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
