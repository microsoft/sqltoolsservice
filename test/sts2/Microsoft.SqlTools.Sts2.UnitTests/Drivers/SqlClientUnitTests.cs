//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.SqlClient;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>Server-free SqlClient adapter logic: connection-string building and error mapping.</summary>
    public class SqlClientUnitTests
    {
        private static ConnectionOpenRequest Request(SecretMaterial auth, Dictionary<string, string>? options = null) => new()
        {
            Server = "tcp:host,1433",
            Database = "appdb",
            Auth = auth,
            ConnectTimeoutMs = 15000,
            ApplicationName = "sts2-test",
            Options = options ?? new Dictionary<string, string>(),
        };

        private static ColumnInfo Column(string engineType, int? length = null) => new()
        {
            Name = "c",
            EngineType = engineType,
            Nullable = true,
            Length = length,
        };

        [Fact]
        public void ClassifyColumnsRoutesVectorToTextByDefaultAndVectorWhenNegotiated() // D-0018/D-0019
        {
            var columns = new List<ColumnInfo> { Column("vector", 20), Column("int"), Column("nvarchar", 50) };

            SqlLargeValueReader.CellRead[] defaults = SqlLargeValueReader.ClassifyColumns(columns);
            Assert.Equal(SqlLargeValueReader.CellRead.Text, defaults[0]);
            Assert.Equal(SqlLargeValueReader.CellRead.Value, defaults[1]);
            Assert.Equal(SqlLargeValueReader.CellRead.Value, defaults[2]); // bounded nvarchar keeps GetValue

            SqlLargeValueReader.CellRead[] negotiated = SqlLargeValueReader.ClassifyColumns(columns, vectorBinary: true);
            Assert.Equal(SqlLargeValueReader.CellRead.Vector, negotiated[0]);
            Assert.Equal(SqlLargeValueReader.CellRead.Value, negotiated[1]);
        }

        [Fact]
        public void ClassifyColumnsRoutesClrUdtsToBinary() // D-0018: GetValue would FileNotFound the whole query
        {
            var columns = new List<ColumnInfo>
            {
                Column("master.sys.geometry", int.MaxValue),
                Column("AppDb.sys.geography", int.MaxValue),
                Column("tempdb.sys.hierarchyid", 892),
                Column("geometry"), // bare name accepted defensively
            };
            SqlLargeValueReader.CellRead[] kinds = SqlLargeValueReader.ClassifyColumns(columns);
            Assert.All(kinds, k => Assert.Equal(SqlLargeValueReader.CellRead.Binary, k));
        }

        [Fact]
        public void EstimateCellBytesCoversVectorAndTruncatedValues() // D-0019 page byte bound
        {
            // A 1,536-dimension float32 vector encodes to ~8.3 KB (base64 of
            // 6144 bytes + tag fields) — the generic fallback would say 24.
            var vector = new DriverVectorValue
            {
                Dimensions = 1536,
                BaseType = "float32",
                Encoding = "f32le",
                ComponentBytes = new byte[6144],
            };
            long vectorEstimate = SqlRowsPageBuilder.EstimateCellBytes(vector);
            Assert.InRange(vectorEstimate, 8192, 8500);

            var truncatedText = new DriverTruncatedValue
            {
                Kind = "string",
                PrefixText = new string('x', 65536),
                TotalBytes = 1_000_000,
                DigestHex = new string('0', 64),
            };
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(truncatedText) >= 65536);

            var truncatedBinary = new DriverTruncatedValue
            {
                Kind = "binary",
                PrefixBytes = new byte[65536],
                TotalBytes = 1_000_000,
                DigestHex = new string('0', 64),
            };
            Assert.True(SqlRowsPageBuilder.EstimateCellBytes(truncatedBinary) >= 87381); // base64 expansion
        }

        [Fact]
        public void VectorRowsPageWithAccurateEstimatesRespectsByteBound() // D-0019 page clamp
        {
            // 32 rows of one 1,536-dim vector each at the pinned 256 KiB page
            // bound: accurate estimates must close pages well before 32 rows
            // (encoded reality ≈ 8.3 KB/row → ~31 rows/page max, and the
            // builder pre-closes when the next row would exceed the bound).
            var builder = new SqlRowsPageBuilder(1000, 262144);
            var vector = new DriverVectorValue
            {
                Dimensions = 1536,
                BaseType = "float32",
                Encoding = "f32le",
                ComponentBytes = new byte[6144],
            };
            int pagesYielded = 0;
            int rowsInFirstPage = 0;
            for (int r = 0; r < 64; r++)
            {
                foreach (var page in builder.Add(new object?[] { 1L, vector }))
                {
                    pagesYielded++;
                    if (pagesYielded == 1)
                    {
                        rowsInFirstPage = page.Count;
                    }
                }
            }
            Assert.True(pagesYielded >= 2, "accurate vector estimates must close pages by bytes");
            Assert.InRange(rowsInFirstPage, 20, 32);
        }

        [Fact]
        public void SqlLoginBuildsUserAndPasswordWithoutLeakingIntoToken()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "sqlLogin", User = "sa", Secret = "p@ss" }));

            Assert.Contains("User ID=sa", connectionString);
            Assert.Contains("Password=p@ss", connectionString); // builder owns this; redaction happens upstream of Core
            Assert.Contains("Initial Catalog=appdb", connectionString);
            Assert.Contains("Application Name=sts2-test", connectionString);
            Assert.Null(token);
        }

        [Fact]
        public void AccessTokenGoesToTokenNotConnectionString()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "accessToken", User = "u", Secret = "jwt-material" }));

            Assert.Equal("jwt-material", token);
            Assert.DoesNotContain("jwt-material", connectionString);
            Assert.DoesNotContain("Password=", connectionString);
        }

        [Fact]
        public void IntegratedSetsIntegratedSecurity()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "integrated" }));
            Assert.Contains("Integrated Security=True", connectionString);
            Assert.Null(token);
        }

        [Theory]
        [InlineData("strict", "Strict")]
        [InlineData("true", "True")]    // Mandatory serializes as True for back-compat
        [InlineData("false", "False")]  // Optional serializes as False
        public void EncryptOptionMapsToBuilderEnum(string optionValue, string expected)
        {
            (string connectionString, _) = SqlClientConnectionString.Build(Request(
                new SecretMaterial { Kind = "integrated" },
                new Dictionary<string, string> { ["encrypt"] = optionValue }));
            Assert.Contains("Encrypt=" + expected, connectionString);
        }

        [Fact]
        public void UnsupportedAuthKindThrowsStableDriverException()
        {
            DbDriverException ex = Assert.Throws<DbDriverException>(() =>
                SqlClientConnectionString.Build(Request(new SecretMaterial { Kind = "kerberosMagic" })));
            Assert.Equal("Sts2.InvalidRequest", ex.Code);
        }
    }
}
