//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.QueryExecution
{
    public class DataTypeTests
    {
        [Test]
        public async Task BigIntTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100 AS BIGINT)", "100");
        }

        [Test]
        public async Task SqlVariantTest()
        {
            await ExecuteAndVerifyResult("DECLARE @ID sql_variant = 90;select @ID", "90");
        }

        [Test]
        public async Task BinaryTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100 AS BINARY)", "0x000000000000000000000000000000000000000000000000000000000064");
        }

        [Test]
        public async Task BitTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(0 AS BIT)", "0");
            await ExecuteAndVerifyResult("SELECT CAST(1 AS BIT)", "1");
        }

        [Test]
        public async Task CharTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('A' AS CHAR(1))", "A");
        }

        [Test]
        public async Task DateTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATE)", "2020-01-01");
        }

        [Test]
        public async Task DateTimeTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIME)", "2020-01-01 00:00:00.000");
        }

        [Test]
        public async Task DateTime2Test()
        {
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIME2)", "2020-01-01 00:00:00.0000000");
        }

        [Test]
        public async Task DateTimeOffsetTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIMEOFFSET)", "2020-01-01 00:00:00.0000000 +00:00");
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIMEOFFSET(6))", "2020-01-01 00:00:00.000000 +00:00");
            await ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIMEOFFSET(0))", "2020-01-01 00:00:00 +00:00");
        }

        [Test]
        public async Task DecimalTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(99999999999999999999999999999999999999 AS DECIMAL(38))", "99999999999999999999999999999999999999");
        }

        [Test]
        public async Task FloatTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100.11 AS FLOAT)", "100.11");
        }

        [Test]
        public async Task ImageTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('0' AS IMAGE)", "0x30");
        }

        [Test]
        public async Task IntTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('100' AS INT)", "100");
        }

        [Test]
        public async Task MoneyTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('100' AS MONEY)", "100.00");
        }

        [Test]
        public async Task NCharTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(N'测' AS NCHAR(1))", "测");
        }

        [Test]
        public async Task NTextTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(N'测试' AS NTEXT)", "测试");
        }

        [Test]
        public async Task NVarCharTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(N'测试' AS NVARCHAR)", "测试");
        }

        [Test]
        public async Task RealTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100 AS REAL)", "100");
        }

        [Test]
        public async Task SmallDateTimeTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('2021-01-01' AS SMALLDATETIME)", "2021-01-01 00:00:00");
        }

        [Test]
        public async Task SmallIntTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100 AS SMALLINT)", "100");
        }

        [Test]
        public async Task SmallMoneyTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(100 AS SMALLMONEY)", "100.00");
        }

        [Test]
        public async Task TextTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('abc' AS TEXT)", "abc");
        }

        [Test]
        public async Task TimeTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('1:00:00.001' AS TIME)", "01:00:00.0010000");
        }

        [Test]
        public async Task TimestampTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('A' AS TIMESTAMP)", "0x4100000000000000");
        }

        [Test]
        public async Task TinyIntTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(1 AS TINYINT)", "1");
        }

        [Test]
        public async Task UniqueIdentifierTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('fbf31d5b-bda2-4907-a50c-5458e95248ae' AS UNIQUEIDENTIFIER)", "fbf31d5b-bda2-4907-a50c-5458e95248ae");
        }

        [Test]
        public async Task VarBinaryTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('ABCD' AS VARBINARY)", "0x41424344");
        }

        [Test]
        public async Task VarCharTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('ABCD' AS VARCHAR)", "ABCD");
        }

        [Test]
        public async Task XmlTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('<ABC>1234</ABC>' AS XML)", "<ABC>1234</ABC>");
        }

        [Test]
        public async Task GeometryTypeTest()
        {
            await ExecuteAndVerifyResult("SELECT geometry::STGeomFromText('POINT (-96.70 40.84)',4326) [Geo]", "0xE6100000010CCDCCCCCCCC2C58C0EC51B81E856B4440");
        }

        [Test]
        public async Task SysnameTypeTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST('testsysname' AS SYSNAME)", "testsysname");
        }

        [Test]
        public async Task HierarchyIdTypeTest()
        {
            await ExecuteAndVerifyResult("SELECT CAST(0x58 as hierarchyid)", "0x58");
        }

        private async Task ExecuteAndVerifyResult(string queryText, string expectedValue)
        {
            // Given a connection to a live database
            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            ConnectionInfo connInfo = result.ConnectionInfo;
            var fileStreamFactory = MemoryFileSystem.GetFileStreamFactory();
            Query query = new Query(queryText, connInfo, new QueryExecutionSettings(), fileStreamFactory);
            query.Execute();
            query.ExecutionTask.Wait();
            var subset = await query.GetSubset(0, 0, 0, 10);
            Assert.AreEqual(1, subset.RowCount, "Row count does not match expected value.");
            var actualValue = subset.Rows[0][0].DisplayValue;
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}