//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Management.Common;
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
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "SqlAzure"), Is.EqualTo("SqlAzureDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "SingleInstance"), Is.EqualTo("Standalone"));
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "sqlazure"), Is.EqualTo("SqlAzureDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "singleinstance"), Is.EqualTo("Standalone"));

            // The mapped values must be parseable by the core SMO enum.
            Assert.That(System.Enum.Parse<DatabaseEngineType>(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "SqlAzure"), ignoreCase: true),
                Is.EqualTo(DatabaseEngineType.SqlAzureDatabase));
            Assert.That(System.Enum.Parse<DatabaseEngineType>(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "SingleInstance"), ignoreCase: true),
                Is.EqualTo(DatabaseEngineType.Standalone));
        }

        [Test]
        [TestCase("SqlAzureDatabaseEdition", "SqlDatabase")]
        [TestCase("sqlazuredatabaseedition", "SqlDatabase")]
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
            string mapped = SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineEdition), input);
            Assert.That(mapped, Is.EqualTo(expected));

            // When the mapped name exists in the referenced SMO enum, it must parse successfully.
            // Some newer editions may not be present in every SMO version, so only assert for known names.
            if (System.Array.Exists(System.Enum.GetNames<DatabaseEngineEdition>(), n => string.Equals(n, mapped, System.StringComparison.OrdinalIgnoreCase)))
            {
                Assert.That(System.Enum.IsDefined(System.Enum.Parse<DatabaseEngineEdition>(mapped, ignoreCase: true)), Is.True);
            }
        }

        /// <summary>
        /// Values that are not part of the known mapping table (including values for other
        /// properties) must be returned unchanged so existing behavior is preserved.
        /// </summary>
        [Test]
        public void MapEnumValuePassesThroughUnmappedValues()
        {
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineType), "SqlAzureDatabase"), Is.EqualTo("SqlAzureDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue(nameof(ScriptOptions.TargetDatabaseEngineEdition), "SqlDatabase"), Is.EqualTo("SqlDatabase"));
            Assert.That(SmoScriptingOperation.MapEnumValue("ScriptCompatibilityOption", "Script140Compat"), Is.EqualTo("Script140Compat"));
        }

        /// <summary>
        /// IsDefinedEnumName recognizes names that exist on the target enum (case-insensitive) so that
        /// values matching SMO's SqlScriptPublish enums are parsed as-is rather than remapped, while
        /// values that do not exist (the SqlScriptPublish names for the core enums) fall back to mapping.
        /// </summary>
        [Test]
        public void IsDefinedEnumNameDetectsExistingNames()
        {
            // "SqlAzureDatabase" is a member of the core Common.DatabaseEngineType enum.
            Assert.That(SmoScriptingOperation.IsDefinedEnumName(typeof(DatabaseEngineType), "SqlAzureDatabase"), Is.True);
            Assert.That(SmoScriptingOperation.IsDefinedEnumName(typeof(DatabaseEngineType), "sqlazuredatabase"), Is.True);

            // "SqlAzure" is the SqlScriptPublish name and is not part of the core enum.
            Assert.That(SmoScriptingOperation.IsDefinedEnumName(typeof(DatabaseEngineType), "SqlAzure"), Is.False);

            // Non-enum types return false.
            Assert.That(SmoScriptingOperation.IsDefinedEnumName(typeof(string), "SqlAzure"), Is.False);
        }
    }
}
