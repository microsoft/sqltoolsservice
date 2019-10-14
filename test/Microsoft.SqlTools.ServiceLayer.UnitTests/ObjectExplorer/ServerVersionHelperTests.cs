//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
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
            VerifyGetValidForFlag(SqlServerType.SqlOnDemand, ValidForFlag.SqlOnDemand);
        }

        [Fact]
        public void GetValidForFlagShouldReturnTheFlagIncludingSqlDwGivenSqlDwdatabase()
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(SqlServerType.AzureV12, true);
            ValidForFlag expected = ValidForFlag.SqlDw;

            Assert.Equal(validforFlag, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2005Given2005Version()
        {
            string serverVersion = "9.1.2.3";
            SqlServerType expected = SqlServerType.Sql2005;
            VerifyCalculateServerType(serverVersion, expected);

        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2008Given2008Version()
        {
            string serverVersion = "10.1.2.3";
            SqlServerType expected = SqlServerType.Sql2008;
            VerifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2012Given2012Version()
        {
            string serverVersion = "11.1.2.3";
            SqlServerType expected = SqlServerType.Sql2012;
            VerifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2014Given2014Version()
        {
            string serverVersion = "12.1.2.3";
            SqlServerType expected = SqlServerType.Sql2014;
            VerifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2016Given2016Version()
        {
            string serverVersion = "13.1.2.3";
            SqlServerType expected = SqlServerType.Sql2016;
            VerifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSql2017Given2017Version()
        {
            string serverVersion = "14.1.2.3";
            SqlServerType expected = SqlServerType.Sql2017;
            VerifyCalculateServerType(serverVersion, expected);
        }

        [Fact]
        public void IsValidForShouldReturnTrueGivenSqlDwAndAll()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.All;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Fact]
        public void IsValidForShouldReturnTrueGivenSqlDwAndNone()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.None;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Fact]
        public void IsValidForShouldReturnTrueGivenSqlDwAndSqlDw()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.SqlDw;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Fact]
        public void IsValidForShouldReturnTrueGivenSqlDwAndNotSqlDw()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.NotSqlDw;
            bool expected = false;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Fact]
        public void IsValidForShouldReturnTrueGivenSqlDwAndAllOnPrem()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.AllOnPrem;
            bool expected = false;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Fact]
        public void CalculateServerTypeShouldReturnSqlOnDemandGivenEngineEdition()
        {
            int engineEdition = 11;
            SqlServerType expected = SqlServerType.SqlOnDemand;
            VerifyCalculateServerTypeForEngineEdition(engineEdition, expected);
        }

        private void VerifyIsValidFor(ValidForFlag serverValidFor, ValidForFlag validFor, bool expected)
        {
            bool actual = ServerVersionHelper.IsValidFor(serverValidFor, validFor);
            Assert.Equal(expected, actual);
        }

        private void VerifyCalculateServerType(string serverVersion, SqlServerType expected)
        {
            ServerInfo serverInfo = new ServerInfo
            {
                ServerVersion = serverVersion
            };
            SqlServerType actual = ServerVersionHelper.CalculateServerType(serverInfo);
            Assert.Equal(expected, actual);
        }

        private void VerifyCalculateServerTypeForEngineEdition(int engineEdition, SqlServerType expected)
        {
            ServerInfo serverInfo = new ServerInfo
            {
                EngineEditionId = engineEdition
            };

            SqlServerType actual = ServerVersionHelper.CalculateServerType(serverInfo);
            
            Assert.True(actual.ToString().Equals(expected.ToString()), "Verify correct engine edition number");
        }

        private void VerifyGetValidForFlag(SqlServerType serverType, ValidForFlag validForFlag)
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(serverType);
            ValidForFlag expected = validForFlag;

            Assert.Equal(validforFlag, expected);
        }
    }
}
