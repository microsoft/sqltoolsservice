//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaCompare
{
    public class SchemaCompareTests
    {
        [Test]
        public void FormatScriptAddsGo()
        {
            string script = "EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';";
            Assert.That(script, Does.Not.Contain("GO"));
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.That(result, Does.EndWith("GO"));
        }

        [Test]
        public void FormatScriptDoesNotAddGoForNullScripts()
        {
            string script1 = null;
            string result1 = SchemaCompareUtils.FormatScript(script1);
            Assert.AreEqual(null, result1);

            string script2 = "null";
            string result2 = SchemaCompareUtils.FormatScript(script2);
            Assert.That(result2, Does.Not.Contain("GO"));
        }

        [Test]
        public void FormatScriptDoesNotAddGoForEmptyStringScripts()
        {
            string script = string.Empty;
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void FormatScriptDoesNotAddGoForWhitespaceStringScripts()
        {
            string script = "    \t\n";
            Assert.True(string.IsNullOrWhiteSpace(script));
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void RemovesExcessWhitespace()
        {
            // leading whitespace
            string script1 = "\r\n   EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';";
            string result1 = SchemaCompareUtils.RemoveExcessWhitespace(script1);
            Assert.False(script1.Equals(result1));
            Assert.False(result1.StartsWith("\r"));
            Assert.True(result1.StartsWith("EXECUTE"));

            // trailing whitespace
            string script2 = "EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';  \n";
            string result2 = SchemaCompareUtils.RemoveExcessWhitespace(script2);
            Assert.False(script2.Equals(result2));
            Assert.False(result2.EndsWith("\n"));
            Assert.True(result2.EndsWith(";"));

            // non-leading/trailing multiple spaces
            string script3 = @"CREATE TABLE [dbo].[AWBuildVersion] (
     [SystemInformationID] TINYINT IDENTITY (1, 1) NOT NULL,
 [Database Version] NVARCHAR (25)     NOT NULL,
 [VersionDate] DATETIME     NOT NULL,
 [ModifiedDate] DATETIME NOT NULL
);";
            string expected3 = @"CREATE TABLE [dbo].[AWBuildVersion] (
 [SystemInformationID] TINYINT IDENTITY (1, 1) NOT NULL,
 [Database Version] NVARCHAR (25) NOT NULL,
 [VersionDate] DATETIME NOT NULL,
 [ModifiedDate] DATETIME NOT NULL
);";
            string result3 = SchemaCompareUtils.RemoveExcessWhitespace(script3);
            Assert.True(expected3.Equals(result3));
        }

        [Test]
        public void CreateExcludedObjects()
        {
            //successful creation
            ValidateTableCreation(new string[] { "dbo", "Table1" }, "dbo.Table1");
            ValidateTableCreation(new string[] { "[dbo]", "Table.1" }, "[dbo].Table.1");

            //null creation due to null name
            SchemaCompareObjectId object1 = new SchemaCompareObjectId
            {
                NameParts = null, //null caused by this value
                SqlObjectType = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable"
            };

            var nullResult1 = SchemaCompareUtils.CreateExcludedObject(object1);
            Assert.Null(nullResult1);

            //null creation due to argumentException
            SchemaCompareObjectId object2 = new SchemaCompareObjectId
            {
                NameParts = new string[] { "dbo", "Table1" },
                SqlObjectType = "SqlTable" // null caused by this value
            };

            var nullResult2 = SchemaCompareUtils.CreateExcludedObject(object2);
            Assert.Null(nullResult2);
        }

        private void ValidateTableCreation(string[] nameParts, string validationString)
        {
            SchemaCompareObjectId validObject1 = new SchemaCompareObjectId
            {
                NameParts = nameParts,
                SqlObjectType = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable"
            };
            var validResult1 = SchemaCompareUtils.CreateExcludedObject(validObject1);
            Assert.NotNull(validResult1);
            Assert.AreEqual(validObject1.SqlObjectType, validResult1.TypeName);
            Assert.AreEqual(validObject1.NameParts.Length, validResult1.Identifier.Parts.Count);
            Assert.AreEqual(validationString, string.Join(".", validResult1.Identifier.Parts));
            for (int i = 0; i < validObject1.NameParts.Length; i++)
            {
                Assert.AreEqual(validObject1.NameParts[i], validResult1.Identifier.Parts[i]);
            }
        }

        [Test]
        public void SchemaCompareResultExposesSourceAndTargetPlatformProperties()
        {
            // The SourcePlatform / TargetPlatform projection lets clients show the user
            // which T-SQL dialect Schema Compare is actually running under (e.g. "SqlDwUnified"
            // for Fabric Warehouse vs. "Sql160" for Azure SQL Database). The values are
            // sourced from SchemaCompareUtils.GetComparisonPlatform, which reads
            // DatabaseSchemaProvider.Platform via reflection because TSqlModel.Version
            // reports "Sql150" for Fabric Warehouse models. The meaningful unit-level
            // guarantee is that the contract surface accepts and round-trips the values;
            // end-to-end behavior for live comparisons is covered by integration tests.
            SchemaCompareResult result = new SchemaCompareResult
            {
                OperationId = "op-1",
                Success = true,
                AreEqual = false,
                SourcePlatform = "SqlDwUnified",
                TargetPlatform = "SqlDwUnified",
            };

            Assert.AreEqual("SqlDwUnified", result.SourcePlatform);
            Assert.AreEqual("SqlDwUnified", result.TargetPlatform);

            // Default values must be null so existing clients that ignore the field continue
            // to behave as before and JSON serializers omit the property when the comparison
            // did not produce a model (e.g. failed validation).
            SchemaCompareResult defaultResult = new SchemaCompareResult();
            Assert.IsNull(defaultResult.SourcePlatform);
            Assert.IsNull(defaultResult.TargetPlatform);
        }

        [Test]
        public void SchemaCompareOperationExposesSourceAndTargetPlatformProperties()
        {
            // The operation surfaces Source/TargetPlatform after Execute() so
            // SchemaCompareService can wire it into the SchemaCompareResult contract without
            // re-walking the DacFx model on the JSON-RPC layer. The values are populated by
            // Execute() via SchemaCompareUtils.GetComparisonPlatform (DSP-based reflection),
            // not from TSqlModel.Version (which misreports Fabric Warehouse as Sql150). This
            // test exercises the contract's get/set surface; the reflection-based population
            // is covered by the SchemaCompareFabricTests integration suite.
            SchemaCompareParams parameters = new SchemaCompareParams { OperationId = "op-2" };
            SchemaCompareOperation operation = new SchemaCompareOperation(parameters, connectionProvider: null);

            // Properties exist and are writable by the Execute() path.
            Assert.IsNull(operation.SourcePlatform);
            Assert.IsNull(operation.TargetPlatform);

            operation.SourcePlatform = "Sql160";
            operation.TargetPlatform = "SqlDwUnified";

            Assert.AreEqual("Sql160", operation.SourcePlatform);
            Assert.AreEqual("SqlDwUnified", operation.TargetPlatform);
        }

        // -----------------------------------------------------------------------------
        // Tests for the Fabric Warehouse (SqlDwUnified) constraint-script preservation
        // decision. The pure helper ShouldPreserveAlterScriptForConstraint is the unit
        // we can exercise here without manufacturing a real SchemaComparisonResult graph
        // (DacFx's comparison types are sealed and require live model loading). Real
        // CreateDiffEntry coverage is provided by SchemaCompareFabricTests in the
        // IntegrationTests project, where actual Fabric-DSP dacpacs are compared.
        // -----------------------------------------------------------------------------

        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlForeignKeyConstraint")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlUniqueConstraint")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlCheckConstraint")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlDefaultConstraint")]
        public void ShouldPreserveAlterScriptForConstraint_TrueForEveryConstraintKindUnderSqlDwUnified(string objectTypeName)
        {
            // Each of the five hierarchical-child constraint kinds (PK / FK / UNIQUE / CHECK /
            // DEFAULT) is always emitted as a standalone ALTER TABLE script on SqlDwUnified
            // — never inlined into CREATE TABLE — so the diff entry's ALTER script is the
            // only place the constraint is defined. Stripping it would leave Fabric Warehouse
            // diffs missing the constraint definition entirely.
            Assert.IsTrue(
                SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(objectTypeName, "SqlDwUnified"),
                $"Expected ALTER script to be preserved for {objectTypeName} on SqlDwUnified");
        }

        [TestCase("Sql160")]
        [TestCase("Sql150")]
        [TestCase("SqlAzure")]
        [TestCase("SqlAzureV12")]
        [TestCase("SqlDw")]
        public void ShouldPreserveAlterScriptForConstraint_FalseForConstraintsOnNonFabricPlatforms(string platform)
        {
            // Strip-on-alter behaviour is correct on SQL Server / Azure SQL / Synapse: those
            // dialects inline PK/FK/UNIQUE/CHECK/DEFAULT into CREATE TABLE, so the child
            // ALTER scripts are redundant duplicates. The helper must keep returning false
            // for every non-Fabric platform string a comparison can produce.
            const string pkConstraint = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint";
            Assert.IsFalse(
                SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(pkConstraint, platform),
                $"Expected ALTER script to be stripped for PK constraint on non-Fabric platform '{platform}'");
        }

        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlSimpleColumn")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlIndex")]
        [TestCase("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlView")]
        public void ShouldPreserveAlterScriptForConstraint_FalseForNonConstraintObjectTypesUnderSqlDwUnified(string objectTypeName)
        {
            // Even on SqlDwUnified the legacy strip-on-alter rule must continue to apply to
            // non-constraint hierarchical children (column changes, indexes, view body
            // updates, etc.) — those ALTER scripts really are duplicates of the parent's
            // CREATE / ALTER, and re-emitting them would produce broken generated scripts.
            Assert.IsFalse(
                SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(objectTypeName, "SqlDwUnified"),
                $"Expected ALTER script to be stripped for non-constraint type {objectTypeName} on SqlDwUnified");
        }

        [Test]
        public void ShouldPreserveAlterScriptForConstraint_FalseForNullOrEmptyTypeName()
        {
            // The reflection helper that supplies the type name can legitimately return null
            // for malformed DiffEntries; this branch must fail safe (preserve legacy
            // strip-on-alter behaviour) rather than throw.
            Assert.IsFalse(SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(null, "SqlDwUnified"));
            Assert.IsFalse(SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(string.Empty, "SqlDwUnified"));
        }

        [Test]
        public void ShouldPreserveAlterScriptForConstraint_FalseForNullOrEmptyPlatform()
        {
            // The reflection-based platform lookup returns null when DacFx internals shift
            // (e.g. across NuGet upgrades). The fallback path must preserve the legacy
            // strip-on-alter behaviour rather than over-eagerly keep constraints on
            // unknown platforms.
            const string pkConstraint = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint";
            Assert.IsFalse(SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(pkConstraint, null));
            Assert.IsFalse(SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(pkConstraint, string.Empty));
        }

        [Test]
        public void ShouldPreserveAlterScriptForConstraint_RequiresEndsWithMatchNotSubstring()
        {
            // The helper matches by suffix to mirror DacFx's full-type-name convention
            // ("Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint"). A
            // substring match would produce false positives like the (hypothetical) wrapper
            // "...SqlPrimaryKeyConstraintAction"; lock the contract by asserting it does not.
            Assert.IsFalse(
                SchemaCompareUtils.ShouldPreserveAlterScriptForConstraint(
                    "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraintAction",
                    "SqlDwUnified"));
        }

        [Test]
        public void GetComparisonPlatform_ReturnsNullForNullResult()
        {
            // The reflection helper must short-circuit on a null result rather than
            // NullReferenceException. SchemaCompareOperation.Execute may pass null on
            // failure paths.
            Assert.IsNull(SchemaCompareUtils.GetComparisonPlatform(null));
        }
    }
}
