//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
        public void BigIntTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100 AS BIGINT)", "100");
        }

        [Test]
        public void BinaryTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100 AS BINARY)", "0x000000000000000000000000000000000000000000000000000000000064");
        }

        [Test]
        public void BitTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(0 AS BIT)", "0");
            ExecuteAndVerifyResult("SELECT CAST(1 AS BIT)", "1");
        }

        [Test]
        public void CharTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('A' AS CHAR)", "A");
        }

        [Test]
        public void DateTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATE)", "2020-01-01");
        }

        [Test]
        public void DateTimeTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIME)", "2020-01-01 00:00:00.000");
        }

        [Test]
        public void DateTime2Test()
        {
            ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIME2)", "2020-01-01 00:00:00.0000000");
        }

        [Test]
        public void DateTimeOffsetTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('2020-01-01' AS DATETIMEOFFSET)", "2020-01-01 00:00:00.0000000 +00:00");
        }

        [Test]
        public void DecimalTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(99999999999999999999999999999999999999 AS DECIMAL(38))", "99999999999999999999999999999999999999");
        }

        [Test]
        public void FloatTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100.11 AS FLOAT)", "100.11");
        }

        [Test]
        public void ImageTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('0' AS IMAGE)", "0X30");
        }

        [Test]
        public void IntTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('100' AS INT)", "100");
        }

        [Test]
        public void MoneyTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('100' AS MONEY)", "100.00");
        }

        [Test]
        public void NCharTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(N'测' AS NCHAR(1))", "测");
        }

        [Test]
        public void NTextTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(N'测试' AS NTEXT)", "测试");
        }

        [Test]
        public void NVarCharTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(N'测试' AS NVARCHAR)", "测试");
        }

        [Test]
        public void RealTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100 AS REAL)", "100");
        }

        [Test]
        public void SmallDateTimeTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('2021-01-01' AS SMALLDATETIME)", "2021-01-01 00:00:00");
        }

        [Test]
        public void SmallIntTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100 AS SMALLINT)", "100");
        }

        [Test]
        public void SmallMoneyTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(100 AS SMALLMONEY)", "100.00");
        }

        [Test]
        public void TextTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('abc' AS TEXT)", "abc");
        }

        [Test]
        public void TimeTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('1:00:00' AS TIME)", "01:00:00.0000000");
        }

        [Test]
        public void TimestampTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('a' AS TIMESTAMP)", "0x4100000000000000");
        }

        [Test]
        public void TinyIntTest()
        {
            ExecuteAndVerifyResult("SELECT CAST(1 AS TINYINT)", "1");
        }

        [Test]
        public void UniqueIdentifierTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('fbf31d5b-bda2-4907-a50c-5458e95248ae' AS UNIQUEIDENTIFIER)", "FBF31D5B-BDA2-4907-A50C-5458E95248AE");
        }

        [Test]
        public void VarBinaryTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('ABCD' AS VARBINARY)", "0x41424344");
        }

        [Test]
        public void VarCharTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('ABCD' AS VARCHAR)", "ABCD");
        }

        [Test]
        public void XmlTest()
        {
            ExecuteAndVerifyResult("SELECT CAST('<ABC>1234</ABC>' AS XML)", "<ABC>1234</ABC>");
        }

        private async void ExecuteAndVerifyResult(string queryText, string expectedValue)
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