//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.SqlCore.Scripting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Scripting
{
    public class SmoScriptingOperationTests
    {
        /// <summary>
        /// The SqlScriptPublish enum value "SqlAzure" must be mapped to the core SMO
        /// DatabaseEngineType value "SqlAzureDatabase" so that Enum.Parse succeeds and
        /// SMO scripts Azure SQL DB objects (such as temporal tables) correctly.
        /// </summary>
        [Test]
        public void MapEnumValueMapsTargetDatabaseEngineType()
        {
            Assert.That(SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineType", "SqlAzure"), Is.EqualTo("SqlAzureDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineType", "SingleInstance"), Is.EqualTo("Standalone"));

            // The mapped values must be parseable by the core SMO enum.
            Assert.That(System.Enum.Parse(typeof(DatabaseEngineType), SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineType", "SqlAzure"), ignoreCase: true),
                Is.EqualTo(DatabaseEngineType.SqlAzureDatabase));
            Assert.That(System.Enum.Parse(typeof(DatabaseEngineType), SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineType", "SingleInstance"), ignoreCase: true),
                Is.EqualTo(DatabaseEngineType.Standalone));
        }

        [Test]
        [TestCase("SqlAzureDatabaseEdition", "SqlDatabase")]
        [TestCase("SqlDatawarehouseEdition", "SqlDataWarehouse")]
        [TestCase("SqlServerStretchEdition", "SqlStretchDatabase")]
        [TestCase("SqlServerManagedInstanceEdition", "SqlManagedInstance")]
        [TestCase("SqlServerOnDemandEdition", "SqlOnDemand")]
        [TestCase("SqlServerPersonalEdition", "Personal")]
        [TestCase("SqlServerStandardEdition", "Standard")]
        [TestCase("SqlServerEnterpriseEdition", "Enterprise")]
        [TestCase("SqlServerExpressEdition", "Express")]
        [TestCase("SqlDatabaseEdgeEdition", "SqlDatabaseEdge")]
        [TestCase("SqlAzureArcManagedInstanceEdition", "SqlAzureArcManagedInstance")]
        [TestCase("SqlFabricSqlDatabaseEdition", "FabricSqlDatabase")]
        public void MapEnumValueMapsTargetDatabaseEngineEdition(string input, string expected)
        {
            string mapped = SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineEdition", input);
            Assert.That(mapped, Is.EqualTo(expected));

            // The mapped values must be parseable by the core SMO enum.
            Assert.That(System.Enum.IsDefined(typeof(DatabaseEngineEdition), System.Enum.Parse(typeof(DatabaseEngineEdition), mapped, ignoreCase: true)), Is.True);
        }

        /// <summary>
        /// Values that are not part of the known mapping table (including values for other
        /// properties) must be returned unchanged so existing behavior is preserved.
        /// </summary>
        [Test]
        public void MapEnumValuePassesThroughUnmappedValues()
        {
            Assert.That(SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineType", "SqlAzureDatabase"), Is.EqualTo("SqlAzureDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue("TargetDatabaseEngineEdition", "SqlDatabase"), Is.EqualTo("SqlDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue("ScriptCompatibilityOption", "Script140Compat"), Is.EqualTo("Script140Compat"));
        }
    }
}
