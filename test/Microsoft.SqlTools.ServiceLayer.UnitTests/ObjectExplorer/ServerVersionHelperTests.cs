//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class ServerVersionHelperTests
    {
        [Test]
        public void GetValidForFlagShouldReturnAllGivenUnKnownVersion()
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(SqlServerType.Unknown);
            ValidForFlag expected = ValidForFlag.All;

            Assert.AreEqual(validforFlag, expected);
        }

        [TestCase(SqlServerType.AzureV12, ValidForFlag.AzureV12)]
        [TestCase(SqlServerType.Sql2005, ValidForFlag.Sql2005)]
        [TestCase(SqlServerType.Sql2008, ValidForFlag.Sql2008)]
        [TestCase(SqlServerType.Sql2012, ValidForFlag.Sql2012)]
        [TestCase(SqlServerType.Sql2014, ValidForFlag.Sql2014)]
        [TestCase(SqlServerType.Sql2016, ValidForFlag.Sql2016)]
        [TestCase(SqlServerType.Sql2017, ValidForFlag.Sql2017)]
        [TestCase(SqlServerType.Sql2019, ValidForFlag.Sql2019)]
        [TestCase(SqlServerType.Sql2022, ValidForFlag.Sql2022)]
        [TestCase(SqlServerType.SqlOnDemand, ValidForFlag.SqlOnDemand)]
        public void GetValidForFlagShouldReturnTheFlagCorrectlyGivenValidVersion(SqlServerType serverType, ValidForFlag validForFlag)
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(serverType);
            ValidForFlag expected = validForFlag;

            Assert.That(validforFlag, Is.EqualTo(expected));
        }

        [Test]
        public void GetValidForFlagShouldReturnTheFlagIncludingSqlDwGivenSqlDwdatabase()
        {
            ValidForFlag validforFlag = ServerVersionHelper.GetValidForFlag(SqlServerType.AzureV12, true);
            ValidForFlag expected = ValidForFlag.SqlDw;

            Assert.AreEqual(validforFlag, expected);
        }

        [TestCase("9.1.2.3", SqlServerType.Sql2005)]
        [TestCase("10.1.2.3", SqlServerType.Sql2008)]
        [TestCase("11.1.2.3", SqlServerType.Sql2012)]
        [TestCase("12.1.2.3", SqlServerType.Sql2014)]
        [TestCase("13.1.2.3", SqlServerType.Sql2016)]
        [TestCase("14.1.2.3", SqlServerType.Sql2017)]
        [TestCase("15.1.2.3", SqlServerType.Sql2019)]
        [TestCase("16.1.2.3", SqlServerType.Sql2022)]
        public void CalculateServerTypeShouldReturnExpectedValue(string serverVersion, SqlServerType expectedServerType)
        {
            ServerInfo serverInfo = new ServerInfo
            {
                ServerVersion = serverVersion
            };
            SqlServerType actual = ServerVersionHelper.CalculateServerType(serverInfo);
            Assert.That(expectedServerType, Is.EqualTo(actual));
        }

        [Test]
        public void IsValidForShouldReturnTrueGivenSqlDwAndAll()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.All;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Test]
        public void IsValidForShouldReturnTrueGivenSqlDwAndNone()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.None;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Test]
        public void IsValidForShouldReturnTrueGivenSqlDwAndSqlDw()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.SqlDw;
            bool expected = true;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Test]
        public void IsValidForShouldReturnTrueGivenSqlDwAndNotSqlDw()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.NotSqlDw;
            bool expected = false;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Test]
        public void IsValidForShouldReturnTrueGivenSqlDwAndAllOnPrem()
        {
            ValidForFlag serverValidFor = ValidForFlag.SqlDw;
            ValidForFlag validFor = ValidForFlag.AllOnPrem;
            bool expected = false;
            VerifyIsValidFor(serverValidFor, validFor, expected);
        }

        [Test]
        public void CalculateServerTypeShouldReturnSqlOnDemandGivenEngineEdition()
        {
            int engineEdition = 11;
            SqlServerType expected = SqlServerType.SqlOnDemand;
            VerifyCalculateServerTypeForEngineEdition(engineEdition, expected);
        }

        private void VerifyIsValidFor(ValidForFlag serverValidFor, ValidForFlag validFor, bool expected)
        {
            bool actual = ServerVersionHelper.IsValidFor(serverValidFor, validFor);
            Assert.AreEqual(expected, actual);
        }

        private void VerifyCalculateServerTypeForEngineEdition(int engineEdition, SqlServerType expected)
        {
            ServerInfo serverInfo = new ServerInfo
            {
                EngineEditionId = engineEdition
            };

            SqlServerType actual = ServerVersionHelper.CalculateServerType(serverInfo);
            Assert.True(actual == expected, $"Verify server type based on Engine Edition. Actual value: {actual}, Expected value: {expected}");
        }
    }
}
