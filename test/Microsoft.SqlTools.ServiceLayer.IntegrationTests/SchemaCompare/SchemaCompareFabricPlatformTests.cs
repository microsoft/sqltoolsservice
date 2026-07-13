//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Focused Fabric Warehouse (SqlDwUnified) Schema Compare tests that verify the
    /// reflection-free platform surfacing and the constraint ALTER-script carve-out end to
    /// end, using dacpacs built entirely offline (no live Fabric Warehouse required).
    ///
    /// These tests depend on the DacFx version-mapping fix that makes
    /// <c>TSqlModel.Version</c> report <c>SqlDwUnified</c> for Fabric Warehouse models
    /// (previously it returned <c>Sql150</c>, which forced a reflection workaround in STS).
    /// </summary>
    public class SchemaCompareFabricPlatformTests
    {
        // Fabric Warehouse requires NONCLUSTERED / NOT ENFORCED constraints.
        private const string FabricSourceScript = @"
CREATE TABLE [dbo].[Orders] (
    [OrderID] INT          NOT NULL,
    [Sku]     VARCHAR (20) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY NONCLUSTERED ([OrderID]) NOT ENFORCED,
    CONSTRAINT [AK_Orders_Sku] UNIQUE NONCLUSTERED ([Sku]) NOT ENFORCED
);
GO";

        private const string FabricTargetScript = @"
CREATE TABLE [dbo].[Orders] (
    [OrderID] INT          NOT NULL,
    [Sku]     VARCHAR (20) NOT NULL
);
GO";

        // A Fabric Warehouse table graph with PK + UNIQUE + FK, all NONCLUSTERED / NOT ENFORCED
        // as Fabric requires. Used to verify constraints of every kind surface as standalone
        // ALTER TABLE ... ADD CONSTRAINT in the generated deploy script / project write.
        private const string FabricConstraintGraphScript = @"
CREATE TABLE [dbo].[Orders] (
    [OrderID] INT          NOT NULL,
    [Sku]     VARCHAR (20) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY NONCLUSTERED ([OrderID]) NOT ENFORCED,
    CONSTRAINT [AK_Orders_Sku] UNIQUE NONCLUSTERED ([Sku]) NOT ENFORCED
);
GO
CREATE TABLE [dbo].[OrderLines] (
    [OrderLineID] INT NOT NULL,
    [OrderID]     INT NOT NULL,
    CONSTRAINT [PK_OrderLines] PRIMARY KEY NONCLUSTERED ([OrderLineID]) NOT ENFORCED,
    CONSTRAINT [FK_OrderLines_Orders] FOREIGN KEY ([OrderID]) REFERENCES [dbo].[Orders] ([OrderID]) NOT ENFORCED
);
GO";

        /// <summary>
        /// Verifies that a Fabric Warehouse (SqlDwUnified) dacpac-to-dacpac comparison:
        ///   1. surfaces SourcePlatform/TargetPlatform as "SqlDwUnified" read directly from the
        ///      public TSqlModel.Version (no reflection), and
        ///   2. preserves the standalone "ALTER TABLE ... ADD CONSTRAINT" script for the table's
        ///      constraints instead of stripping it (the SqlDwUnified carve-out in CreateDiffEntry).
        /// </summary>
        [Test]
        public void FabricWarehouse_SurfacesPlatformAndPreservesConstraintAlterScript()
        {
            string workingFolder = Path.Combine(Path.GetTempPath(), "SchemaCompareFabricPlatformTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingFolder);
            string sourceDacpac = Path.Combine(workingFolder, "source.dacpac");
            string targetDacpac = Path.Combine(workingFolder, "target.dacpac");

            try
            {
                BuildFabricDacpac(FabricSourceScript, sourceDacpac);
                BuildFabricDacpac(FabricTargetScript, targetDacpac);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = sourceDacpac
                };
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = targetDacpac
                };

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                // dacpac-to-dacpac needs no live connection.
                SchemaCompareOperation operation = new CoreOps.SchemaCompareOperation(schemaCompareParams, new TestConnectionProvider(null, null));
                operation.Execute();

                Assert.IsNull(operation.ErrorMessage, "Fabric dacpac comparison should not error. " + operation.ErrorMessage);
                Assert.IsTrue(operation.ComparisonResult.IsValid, "Fabric dacpac comparison should be valid.");
                Assert.IsFalse(operation.ComparisonResult.IsEqual, "Source adds constraints the target lacks; expected differences.");

                // (1) Reflection-free platform surfacing: read from public TSqlModel.Version.
                Assert.AreEqual("SqlDwUnified", operation.SourcePlatform,
                    "SourcePlatform should be SqlDwUnified (read from ComparisonResult.SourceModel.Version).");
                Assert.AreEqual("SqlDwUnified", operation.TargetPlatform,
                    "TargetPlatform should be SqlDwUnified (read from ComparisonResult.TargetModel.Version).");

                // (2) Constraint carve-out: the primary key surfaces as a diff whose ADD CONSTRAINT
                // script is preserved rather than stripped by the strip-on-alter heuristic.
                DiffEntry pkConstraint = FindDiff(operation.Differences,
                    d => (d.SourceObjectType != null && d.SourceObjectType.EndsWith("PrimaryKeyConstraint", StringComparison.Ordinal)));
                Assert.IsNotNull(pkConstraint, "Expected a diff entry for the PRIMARY KEY constraint on Fabric.");
                Assert.IsFalse(string.IsNullOrEmpty(pkConstraint.SourceScript),
                    "Fabric constraint diff must keep its standalone ALTER script (carve-out), not strip it.");
                StringAssert.Contains("ADD CONSTRAINT", pkConstraint.SourceScript,
                    "Preserved Fabric constraint script should be the standalone ALTER TABLE ... ADD CONSTRAINT.");
            }
            finally
            {
                try { Directory.Delete(workingFolder, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }

        /// <summary>
        /// TEST 1 (fully offline — no live Fabric Warehouse, no live SQL Server).
        ///
        /// Builds a Fabric Warehouse (SqlDwUnified) source containing tables WITH constraints
        /// (PRIMARY KEY + UNIQUE + FOREIGN KEY, all NONCLUSTERED / NOT ENFORCED) and generates the
        /// deployment T-SQL offline by comparing against an empty Fabric target and calling
        /// <c>SchemaComparisonResult.GenerateScript</c>. Verifies the generated Fabric deploy
        /// script:
        ///   1. creates both tables, and
        ///   2. emits every constraint as a standalone <c>ALTER TABLE ... ADD CONSTRAINT ...
        ///      NONCLUSTERED ... NOT ENFORCED</c> statement (the Fabric shape), not inline in
        ///      the CREATE TABLE.
        ///
        /// This exercises the aggregated deploy-script path (GenerateScript), complementing the
        /// per-difference display-script path covered by the other Fabric tests.
        /// </summary>
        [Test]
        public void FabricWarehouse_GeneratesStandaloneConstraintDeployScript_Offline()
        {
            string workingFolder = Path.Combine(Path.GetTempPath(), "SchemaCompareFabricPlatformTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingFolder);
            string sourceDacpac = Path.Combine(workingFolder, "source.dacpac");
            string targetDacpac = Path.Combine(workingFolder, "target.dacpac");

            try
            {
                // Source has the tables + constraints; target is an empty Fabric model.
                BuildFabricDacpac(FabricConstraintGraphScript, sourceDacpac);
                BuildFabricDacpac(string.Empty, targetDacpac);

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = sourceDacpac
                };
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = targetDacpac
                };

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                // dacpac-to-dacpac needs no live connection.
                SchemaCompareOperation operation = new CoreOps.SchemaCompareOperation(schemaCompareParams, new TestConnectionProvider(null, null));
                operation.Execute();

                Assert.IsNull(operation.ErrorMessage, "Fabric dacpac comparison should not error. " + operation.ErrorMessage);
                Assert.IsTrue(operation.ComparisonResult.IsValid, "Fabric dacpac comparison should be valid.");
                Assert.IsFalse(operation.ComparisonResult.IsEqual, "Source adds tables + constraints the empty target lacks; expected differences.");
                Assert.AreEqual("SqlDwUnified", operation.SourcePlatform, "Source should be SqlDwUnified.");
                Assert.AreEqual("SqlDwUnified", operation.TargetPlatform, "Target should be SqlDwUnified.");

                // Generate the deployment script entirely offline against the (empty) Fabric target.
                SchemaCompareScriptGenerationResult scriptResult = operation.ComparisonResult.GenerateScript("FabricTargetDb");

                Assert.IsTrue(scriptResult.Success, "Offline Fabric deploy-script generation should succeed. " + scriptResult.Message);
                Assert.IsFalse(string.IsNullOrWhiteSpace(scriptResult.Script), "Offline Fabric deploy script should not be empty.");

                string script = scriptResult.Script;

                // 1. Both tables are created.
                StringAssert.Contains("CREATE TABLE [dbo].[Orders]", script, "Deploy script should create the Orders table.");
                StringAssert.Contains("CREATE TABLE [dbo].[OrderLines]", script, "Deploy script should create the OrderLines table.");

                // 2. Every constraint is emitted as a standalone ALTER TABLE ... ADD CONSTRAINT
                //    (Fabric never inlines these into CREATE TABLE).
                AssertStandaloneAddConstraint(script, "PK_Orders");
                AssertStandaloneAddConstraint(script, "AK_Orders_Sku");
                AssertStandaloneAddConstraint(script, "PK_OrderLines");
                AssertStandaloneAddConstraint(script, "FK_OrderLines_Orders");

                // Fabric requires NONCLUSTERED / NOT ENFORCED — confirm those survive into the script.
                StringAssert.Contains("NONCLUSTERED", script, "Fabric constraints in the deploy script should be NONCLUSTERED.");
                StringAssert.Contains("NOT ENFORCED", script, "Fabric constraints in the deploy script should be NOT ENFORCED.");
            }
            finally
            {
                try { Directory.Delete(workingFolder, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }

        /// <summary>
        /// TEST 2 (fully offline — no live Fabric Warehouse, no live SQL Server).
        ///
        /// Applies a Fabric Warehouse comparison to a target SQL project and verifies the
        /// constraints round-trip through the project-WRITE path. An offline Fabric SOURCE dacpac
        /// (table + PK + UNIQUE constraints) is compared against an offline EMPTY Fabric TARGET
        /// project (.sqlproj, SqlDwUnified DSP). <c>SchemaComparePublishProjectChangesOperation</c>
        /// (PublishChangesToProject, Flat) writes the changes into the target project; the target
        /// scripts are refreshed and the comparison is re-run. Asserts publish success and that the
        /// re-comparison converges (IsValid &amp;&amp; IsEqual, zero differences) — i.e. the table AND
        /// its standalone constraint ALTER scripts were correctly written into the Fabric project.
        ///
        /// IMPORTANT: PublishChangesToProject uses a DIFFERENT DacFx code path
        /// (AddProjectElement -&gt; GetAggregatedScript -&gt; TryGetScript) than the
        /// GetDiffEntryDisplay*Script accessors, so this genuinely exercises whether Fabric
        /// constraints round-trip through the project-write path.
        /// </summary>
        [Test]
        public void FabricWarehouse_AppliesConstraintsToTargetProject_Offline()
        {
            string workingFolder = Path.Combine(Path.GetTempPath(), "SchemaCompareFabricPlatformTest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingFolder);
            string sourceDacpac = Path.Combine(workingFolder, "source.dacpac");
            string targetProjectFolder = Path.Combine(workingFolder, "TargetFabricProject");
            Directory.CreateDirectory(targetProjectFolder);
            string targetProjectFile = Path.Combine(targetProjectFolder, "TargetFabricProject.sqlproj");

            try
            {
                // Offline Fabric source dacpac with a table + PK + UNIQUE constraints.
                BuildFabricDacpac(FabricSourceScript, sourceDacpac);

                // Offline empty Fabric target project (SqlDwUnified DSP). DacFx only requires the
                // .sqlproj file to exist; the model is built from the DataSchemaProvider string
                // ("DwUnified" -> SqlPlatforms.SqlDwUnified) passed on the endpoint.
                File.WriteAllText(targetProjectFile, EmptyFabricSqlProjXml("TargetFabricProject"));

                SchemaCompareEndpointInfo sourceInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Dacpac,
                    PackageFilePath = sourceDacpac
                };
                SchemaCompareEndpointInfo targetInfo = new SchemaCompareEndpointInfo
                {
                    EndpointType = SchemaCompareEndpointType.Project,
                    ProjectFilePath = targetProjectFile,
                    TargetScripts = Array.Empty<string>(),
                    DataSchemaProvider = FabricDataSchemaProvider,
                    ExtractTarget = DacExtractTarget.Flat
                };

                var schemaCompareParams = new SchemaCompareParams
                {
                    SourceEndpointInfo = sourceInfo,
                    TargetEndpointInfo = targetInfo
                };

                SchemaCompareOperation operation = new CoreOps.SchemaCompareOperation(schemaCompareParams, new TestConnectionProvider(null, null));
                operation.Execute();

                Assert.IsNull(operation.ErrorMessage, "Fabric dacpac-to-project comparison should not error. " + operation.ErrorMessage);
                Assert.IsTrue(operation.ComparisonResult.IsValid, "Fabric dacpac-to-project comparison should be valid.");
                Assert.IsFalse(operation.ComparisonResult.IsEqual, "Source adds a table + constraints the empty target project lacks; expected differences.");
                Assert.AreEqual("SqlDwUnified", operation.SourcePlatform, "Source should be SqlDwUnified.");
                Assert.AreEqual("SqlDwUnified", operation.TargetPlatform, "Target project should be SqlDwUnified.");

                // Apply the changes to the target Fabric project (Flat structure writes .sql files).
                SchemaComparePublishProjectChangesParams publishParams = new SchemaComparePublishProjectChangesParams
                {
                    OperationId = operation.OperationId,
                    TargetProjectPath = targetProjectFolder,
                    TargetFolderStructure = DacExtractTarget.Flat
                };

                SchemaComparePublishProjectChangesOperation publishOperation =
                    new CoreOps.SchemaComparePublishProjectChangesOperation(publishParams, operation.ComparisonResult);
                publishOperation.Execute();

                Assert.IsTrue(publishOperation.PublishResult.Success,
                    "Publishing Fabric changes to the target project should succeed. " + publishOperation.PublishResult.ErrorMessage);
                Assert.IsTrue(publishOperation.PublishResult.AddedFiles.Length >= 1,
                    "Publishing should add at least one .sql file to the target Fabric project.");

                // Refresh the target project's scripts and re-run the comparison.
                string[] writtenScripts = Directory.GetFiles(targetProjectFolder, "*.sql", SearchOption.AllDirectories);
                Assert.IsTrue(writtenScripts.Length >= 1, "The target Fabric project should contain the written .sql file(s).");
                operation.Parameters.TargetEndpointInfo.TargetScripts = writtenScripts;

                operation.Execute();
                (operation.ComparisonResult.Differences as List<SchemaDifference>)?.RemoveAll(d => !d.Included);

                // Convergence: the table AND its standalone constraint ALTER scripts were written
                // into the Fabric project, so the models are now equal with zero differences.
                Assert.IsTrue(operation.ComparisonResult.IsValid, "Re-comparison after applying to the Fabric project should be valid.");
                Assert.IsTrue(operation.ComparisonResult.IsEqual,
                    "After applying to the Fabric project the re-comparison should converge (IsEqual). " +
                    "If not, the table and/or its standalone constraint ALTER scripts did not round-trip through the project-write path. " +
                    DescribeRemainingDifferences(operation.ComparisonResult.Differences));
                Assert.That(operation.ComparisonResult.Differences, Is.Empty,
                    "After applying to the Fabric project there should be zero remaining differences. " +
                    DescribeRemainingDifferences(operation.ComparisonResult.Differences));
            }
            finally
            {
                try { Directory.Delete(workingFolder, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }

        // DataSchemaProvider string that DacFx maps to SqlPlatforms.SqlDwUnified for a project
        // endpoint (SqlPlatformsUtil.GetSqlPlatformByName prepends the "Sql" prefix, so
        // "DwUnified" -> "SqlDwUnified").
        private const string FabricDataSchemaProvider = "DwUnified";

        /// <summary>
        /// Minimal SDK-style .sqlproj XML with a SqlDwUnified DSP, written entirely offline.
        /// DacFx never dotnet-builds this file (it reads the model from the supplied scripts +
        /// DataSchemaProvider), so the SDK reference and DSP element are metadata only.
        /// </summary>
        private static string EmptyFabricSqlProjXml(string projectName)
        {
            return
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"">
  <Sdk Name=""Microsoft.Build.Sql"" Version=""0.1.3-preview"" />
  <PropertyGroup>
    <Name>{projectName}</Name>
    <ProjectGuid>{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}</ProjectGuid>
    <DSP>Microsoft.Data.Tools.Schema.Sql.SqlDwUnifiedDatabaseSchemaProvider</DSP>
    <ModelCollation>1033, CI</ModelCollation>
  </PropertyGroup>
  <Target Name=""BeforeBuild"">
    <Delete Files=""$(BaseIntermediateOutputPath)\project.assets.json"" />
  </Target>
</Project>";
        }

        /// <summary>
        /// Asserts the script emits the named constraint as a standalone
        /// <c>ALTER TABLE ... ADD CONSTRAINT [name]</c> statement (Fabric shape), not inline.
        /// </summary>
        private static void AssertStandaloneAddConstraint(string script, string constraintName)
        {
            int addIdx = script.IndexOf("ADD CONSTRAINT [" + constraintName + "]", StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(addIdx >= 0,
                $"Deploy script should emit a standalone ADD CONSTRAINT for [{constraintName}]. Script:\r\n{script}");

            // The nearest ALTER TABLE before this ADD CONSTRAINT must be closer than the nearest
            // CREATE TABLE, confirming the constraint is added via ALTER TABLE, not inline in CREATE.
            int alterIdx = script.LastIndexOf("ALTER TABLE", addIdx, StringComparison.OrdinalIgnoreCase);
            int createIdx = script.LastIndexOf("CREATE TABLE", addIdx, StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(alterIdx >= 0 && alterIdx > createIdx,
                $"Constraint [{constraintName}] should be added via a standalone ALTER TABLE ... ADD CONSTRAINT, not inline in CREATE TABLE. Script:\r\n{script}");
        }

        /// <summary>
        /// Builds a human-readable summary of any remaining differences, for assertion messages.
        /// </summary>
        private static string DescribeRemainingDifferences(IEnumerable<SchemaDifference> differences)
        {
            if (differences == null)
            {
                return "Remaining differences: (null).";
            }

            var lines = new List<string>();
            CollectDifferenceNames(differences, lines);
            return lines.Count == 0
                ? "Remaining differences: none."
                : "Remaining differences:\r\n  " + string.Join("\r\n  ", lines);
        }

        private static void CollectDifferenceNames(IEnumerable<SchemaDifference> differences, List<string> lines)
        {
            foreach (SchemaDifference diff in differences)
            {
                if (diff == null)
                {
                    continue;
                }
                string name = diff.SourceObject?.Name?.ToString() ?? diff.TargetObject?.Name?.ToString() ?? diff.Name ?? "(unnamed)";
                lines.Add($"{diff.UpdateAction} {name}");
                CollectDifferenceNames(diff.Children, lines);
            }
        }

        private static DiffEntry FindDiff(IEnumerable<DiffEntry> entries, Predicate<DiffEntry> match)
        {
            if (entries == null)
            {
                return null;
            }
            foreach (DiffEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }
                if (match(entry))
                {
                    return entry;
                }
                DiffEntry childMatch = FindDiff(entry.Children, match);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }
            return null;
        }

        /// <summary>
        /// Builds a Fabric Warehouse (SqlDwUnified) dacpac entirely offline from a T-SQL script.
        /// </summary>
        internal static string BuildFabricDacpac(string sqlScript, string dacpacPath)
        {
            using (TSqlModel model = new TSqlModel(SqlServerVersion.SqlDwUnified, new TSqlModelOptions()))
            {
                foreach (string batch in SplitOnGo(sqlScript))
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }
                    model.AddObjects(batch);
                }

                DacPackageExtensions.BuildPackage(
                    dacpacPath,
                    model,
                    new PackageMetadata { Name = "FabricTestPackage", Version = "1.0.0.0", Description = "Fabric Warehouse Schema Compare test fixture" });
            }
            return dacpacPath;
        }

        private static IEnumerable<string> SplitOnGo(string sqlScript)
        {
            if (string.IsNullOrEmpty(sqlScript))
            {
                yield break;
            }

            // Split on lines that are exactly "GO" (case-insensitive), matching how DacFx
            // consumes multi-batch .sql files.
            var current = new System.Text.StringBuilder();
            foreach (string line in sqlScript.Replace("\r\n", "\n").Split('\n'))
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return current.ToString();
                    current.Clear();
                }
                else
                {
                    current.AppendLine(line);
                }
            }
            if (current.Length > 0)
            {
                yield return current.ToString();
            }
        }
    }
}
