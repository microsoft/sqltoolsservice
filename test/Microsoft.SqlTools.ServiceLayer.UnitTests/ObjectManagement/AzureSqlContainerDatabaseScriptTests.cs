//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectManagement
{
    public class AzureSqlContainerDatabaseScriptTests
    {
        [TestCase(false, CompatibilityLevel.Version150, "")]
        [TestCase(true, CompatibilityLevel.Version160, " WITH LEDGER = ON")]
        public void CreateScriptContainsOnlyRequestedOptions(bool isLedger, CompatibilityLevel compatibilityLevel, string ledgerOption)
        {
            string script = DatabaseHandler.CreateAzureSqlContainerDatabaseScript(
                "TestDb", "Latin1_General_100_CS_AS_SC", isLedger, compatibilityLevel);

            string expected = string.Join(Environment.NewLine, new[]
            {
                $"CREATE DATABASE [TestDb] COLLATE Latin1_General_100_CS_AS_SC{ledgerOption};",
                "GO",
                $"ALTER DATABASE [TestDb] SET COMPATIBILITY_LEVEL = {(int)compatibilityLevel};",
                "GO",
                string.Empty
            });
            Assert.That(script, Is.EqualTo(expected));
        }

        [Test]
        public void CreateScriptEscapesDatabaseNameInBothBatches()
        {
            string script = DatabaseHandler.CreateAzureSqlContainerDatabaseScript(
                "Test]Db", "Latin1_General_100_CS_AS_SC", false, CompatibilityLevel.Version160);

            Assert.That(script, Does.Contain("CREATE DATABASE [Test]]Db] COLLATE Latin1_General_100_CS_AS_SC;"));
            Assert.That(script, Does.Contain("ALTER DATABASE [Test]]Db] SET COMPATIBILITY_LEVEL = 160;"));
        }

        [Test]
        public void CreateScriptRejectsCollationSql()
        {
            var exception = Assert.Throws<ArgumentException>(() => DatabaseHandler.CreateAzureSqlContainerDatabaseScript(
                "TestDb", "Latin1_General_100_CS_AS_SC; SELECT 1; --", false, CompatibilityLevel.Version160));

            Assert.That(exception?.ParamName, Is.EqualTo("collationName"));
        }
    }
}
