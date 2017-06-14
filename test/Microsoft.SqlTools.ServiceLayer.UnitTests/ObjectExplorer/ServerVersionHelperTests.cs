//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class ServerVersionHelperTests
    {
        [Fact]
        public void GetValidForFlagShouldReturnAllGivenUnKnownVersion()
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(SqlServerType.Unknown);
            ValidForFlag expected = ValidForFlag.All;

            Assert.Equal(validforFlag, expected);
        }

        [Fact]
        public void GetValidForFlagShouldReturnTheFlagCorrectlyGivenValidVersion()
        {
            VerifyGetValidForFlag(SqlServerType.AzureV12, ValidForFlag.AzureV12);
            VerifyGetValidForFlag(SqlServerType.Sql2005, ValidForFlag.Sql2005);
            VerifyGetValidForFlag(SqlServerType.Sql2008, ValidForFlag.Sql2008);
            VerifyGetValidForFlag(SqlServerType.Sql2012, ValidForFlag.Sql2012);
            VerifyGetValidForFlag(SqlServerType.Sql2014, ValidForFlag.Sql2014);
            VerifyGetValidForFlag(SqlServerType.Sql2016, ValidForFlag.Sql2016);
            VerifyGetValidForFlag(SqlServerType.Sql2017, ValidForFlag.Sql2017);
        }

        [Fact]
        public void GetValidForFlagShouldReturnTheFlagIncludingSqlDwGivenSqlDwdatabase()
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(SqlServerType.AzureV12, true);
            ValidForFlag expected = ValidForFlag.AzureV12 | ValidForFlag.SqlDw;

            Assert.Equal(validforFlag, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2005Given2005Version()
        {
            string serverVersion = "9.1.2.3";
            SqlServerType expected = SqlServerType.Sql2005;
            VrifyCalculateServerType(serverVersion, expected);

        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2008Given2008Version()
        {
            string serverVersion = "10.1.2.3";
            SqlServerType expected = SqlServerType.Sql2008;
            VrifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2012Given2012Version()
        {
            string serverVersion = "11.1.2.3";
            SqlServerType expected = SqlServerType.Sql2012;
            VrifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2014Given2014Version()
        {
            string serverVersion = "12.1.2.3";
            SqlServerType expected = SqlServerType.Sql2014;
            VrifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2016Given2016Version()
        {
            string serverVersion = "13.1.2.3";
            SqlServerType expected = SqlServerType.Sql2016;
            VrifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2017Given2017Version()
        {
            string serverVersion = "14.1.2.3";
            SqlServerType expected = SqlServerType.Sql2017;
            VrifyCalculateServerType(serverVersion, expected);
        }

        private void VrifyCalculateServerType(string serverVersion, SqlServerType expected)
        {
            ServerInfo serverInfo = new ServerInfo
            {
                ServerVersion = serverVersion
            };
            SqlServerType actual = ServerVersionHelper.CalculateServerType(serverInfo);
            Assert.Equal(expected, actual);
        }

        private void VerifyGetValidForFlag(SqlServerType serverType, ValidForFlag validForFlag)
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(serverType);
            ValidForFlag expected = validForFlag;

            Assert.Equal(validforFlag, expected);
        }
    }
}
