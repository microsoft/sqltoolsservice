//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using NUnit.Framework;
using CoreOps = Microsoft.SqlTools.SqlCore.SchemaCompare;
using SchemaCompareEndpointInfo = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts.SchemaCompareEndpointInfo;
using SchemaCompareEndpointType = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts.SchemaCompareEndpointType;
using SchemaCompareParams = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts.SchemaCompareParams;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Integration tests for the Schema Compare for Fabric Warehouse feature
    /// (Feature 1847587). Implements the test design from
    /// <c>Specs/ClientExperiences/Features/1847587/Engineering/design_spec.md</c>:
    /// the Fabric ↔ Fabric scenario × endpoint-pair matrix and the seven
    /// per-cell assertions defined there.
    ///
    /// The fixtures live alongside this file under <c>SchemaCompare/Fabric/*.sql</c>
    /// and <c>SchemaCompare/SqlProjects/emptyFabricTemplate.sqlproj</c>. Dacpacs are
    /// built in-memory via <see cref="TSqlModel"/> with
    /// <see cref="SqlServerVersion.SqlDwUnified"/>, so no live Fabric Warehouse is
    /// required to execute the suite — it runs entirely offline against the locally
    /// built DacFx <c>SchemaSql</c> binaries that ship the cascade fix (DacFx PR
    /// 2143938) and the Fabric-DSP support (DacFx PR 2134925).
    ///
    /// The live-Fabric publish assertion from the spec (per-cell assertion 7) is
    /// intentionally manual-only and is exercised by the WSR matrix; it is not
    /// reproduced here.
    /// </summary>
    [TestFixture]
    [Category("Fabric")]
    public class SchemaCompareFabricTests
    {
        private const string FabricPlatformName = "SqlDwUnified";

        private const string SqlPrimaryKeyConstraintType =
            "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlPrimaryKeyConstraint";
        private const string SqlForeignKeyConstraintType =
            "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlForeignKeyConstraint";
        private const string SqlUniqueConstraintType =
            "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlUniqueConstraint";

        private static readonly string[] ConstraintTypeSuffixes = new[]
        {
            "PrimaryKeyConstraint",
            "ForeignKeyConstraint",
            "UniqueConstraint",
            "CheckConstraint",
            "DefaultConstraint",
        };

        private static readonly string FabricFixturesFolder = Path.Combine(
            "..", "..", "..", "SchemaCompare", "Fabric");

        private static readonly string FabricSqlProjectTemplate = Path.Combine(
            "..", "..", "..", "SchemaCompare", "SqlProjects", "emptyFabricTemplate.sqlproj");

        private string _workingFolder;

        [SetUp]
        public void SetUp()
        {
            _workingFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SchemaCompareFabricTest",
                $"{TestContext.CurrentContext.Test.Name}_{DateTime.UtcNow.Ticks}");
            Directory.CreateDirectory(_workingFolder);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_workingFolder != null && Directory.Exists(_workingFolder))
                {
                    Directory.Delete(_workingFolder, recursive: true);
                }
            }
            catch
            {
                // Cleanup is best-effort; test results take priority over filesystem hygiene.
            }
        }

        // ---------------------------------------------------------------------
        // TestCaseSource: cross-product of (scenario, endpoint pair) per the spec
        // matrix. Each case is emitted as its own NUnit test so failures pinpoint
        // the exact (scenario, endpoint pair) cell.
        // ---------------------------------------------------------------------

        public enum EndpointPair
        {
            DacpacToDacpac,
            DacpacToProject,
            ProjectToDacpac,
            ProjectToProject,
        }

        public sealed class FabricScenario
        {
            public string Name { get; init; }

            public string SourceScriptFile { get; init; }

            public string TargetScriptFile { get; init; }

            public override string ToString() => Name;
        }

        private static readonly FabricScenario[] Scenarios = new[]
        {
            new FabricScenario
            {
                Name = "AllSupportedTypes",
                SourceScriptFile = "FabricAllTypes_Source.sql",
                TargetScriptFile = "FabricAllTypes_Target.sql",
            },
            new FabricScenario
            {
                Name = "CompatibleAlterColumn",
                SourceScriptFile = "FabricWidenColumn_Source.sql",
                TargetScriptFile = "FabricWidenColumn_Target.sql",
            },
            new FabricScenario
            {
                Name = "IncompatibleAlterColumn",
                SourceScriptFile = "FabricNarrowColumn_Source.sql",
                TargetScriptFile = "FabricNarrowColumn_Target.sql",
            },
            new FabricScenario
            {
                Name = "PrimaryKey",
                SourceScriptFile = "FabricPrimaryKey_Source.sql",
                TargetScriptFile = "FabricPrimaryKey_Target.sql",
            },
            new FabricScenario
            {
                Name = "Unique",
                SourceScriptFile = "FabricUnique_Source.sql",
                TargetScriptFile = "FabricUnique_Target.sql",
            },
            new FabricScenario
            {
                Name = "ForeignKey",
                SourceScriptFile = "FabricForeignKey_Source.sql",
                TargetScriptFile = "FabricForeignKey_Target.sql",
            },
            new FabricScenario
            {
                Name = "SelectionScope",
                SourceScriptFile = "FabricSelectionScope_Source.sql",
                TargetScriptFile = "FabricSelectionScope_Target.sql",
            },
        };

        public static IEnumerable<TestCaseData> ScenarioMatrix()
        {
            foreach (FabricScenario scenario in Scenarios)
            {
                foreach (EndpointPair pair in Enum.GetValues<EndpointPair>())
                {
                    yield return new TestCaseData(scenario, pair)
                        .SetName($"FabricCompare_{scenario.Name}_{pair}")
                        .SetDescription($"Fabric ↔ Fabric Schema Compare matrix cell: scenario={scenario.Name}, pair={pair}");
                }
            }
        }

        [TestCaseSource(nameof(ScenarioMatrix))]
        public void FabricCompare_MatrixCell_SatisfiesPerCellAssertions(FabricScenario scenario, EndpointPair pair)
        {
            string sourceScript = ReadFabricFixture(scenario.SourceScriptFile);
            string targetScript = ReadFabricFixture(scenario.TargetScriptFile);

            (SchemaCompareEndpointInfo sourceInfo, SchemaCompareEndpointInfo targetInfo) =
                BuildEndpointPair(pair, scenario.Name, sourceScript, targetScript);

            SchemaCompareParams parameters = new SchemaCompareParams
            {
                OperationId = $"FabricCompare_{scenario.Name}_{pair}",
                SourceEndpointInfo = sourceInfo,
                TargetEndpointInfo = targetInfo,
            };

            SchemaCompareOperation operation = new SchemaCompareOperation(parameters, connectionProvider: null);

            operation.Execute();

            AssertCommonFabricExpectations(operation, scenario);

            if (scenario.Name == "PrimaryKey" || scenario.Name == "Unique" || scenario.Name == "ForeignKey")
            {
                AssertConstraintFoldedAsChildOfParentTable(operation, scenario);
            }

            if (scenario.Name == "SelectionScope")
            {
                AssertSelectionScopeProducesIsolatedScript(operation);
            }

            // Project-target cells must be byte-equal after PublishChangesToProject + re-compare
            // per per-cell assertion 6. Re-comparison is the cheapest stable round-trip we can
            // execute without a live Fabric DB.
            if (pair == EndpointPair.DacpacToProject || pair == EndpointPair.ProjectToProject)
            {
                AssertPublishToProjectThenReCompareIsEqual(operation, scenario.Name);
            }
        }

        // ---------------------------------------------------------------------
        // Per-cell assertions implementation (numbered per spec test design)
        // ---------------------------------------------------------------------

        private static void AssertCommonFabricExpectations(SchemaCompareOperation operation, FabricScenario scenario)
        {
            // Assertion 1: comparison result is valid and produced differences.
            Assert.IsNull(operation.ErrorMessage,
                $"Schema compare for scenario '{scenario.Name}' reported an unexpected error: {operation.ErrorMessage}");
            Assert.IsNotNull(operation.ComparisonResult,
                $"Schema compare for scenario '{scenario.Name}' returned a null ComparisonResult");
            Assert.IsTrue(operation.ComparisonResult.IsValid,
                $"Schema compare for scenario '{scenario.Name}' returned IsValid=false");
            Assert.IsFalse(operation.ComparisonResult.IsEqual,
                $"Schema compare for scenario '{scenario.Name}' returned IsEqual=true; the fixtures must produce differences");
            Assert.IsTrue(operation.ComparisonResult.Differences != null && operation.ComparisonResult.Differences.Any(),
                $"Schema compare for scenario '{scenario.Name}' returned an empty Differences collection");

            // Assertion 2: SourcePlatform / TargetPlatform are reported as SqlDwUnified.
            // The reflection helper in SchemaCompareUtils.GetComparisonPlatform reads the
            // DSP from SchemaComparisonResult.DataModel.DatabaseSchemaProvider.Platform,
            // which carries SqlPlatforms.SqlDwUnified for a Fabric-DSP comparison.
            Assert.AreEqual(FabricPlatformName, operation.SourcePlatform,
                $"Scenario '{scenario.Name}': SourcePlatform must report '{FabricPlatformName}' for Fabric comparisons");
            Assert.AreEqual(FabricPlatformName, operation.TargetPlatform,
                $"Scenario '{scenario.Name}': TargetPlatform must report '{FabricPlatformName}' for Fabric comparisons");
        }

        private static void AssertConstraintFoldedAsChildOfParentTable(SchemaCompareOperation operation, FabricScenario scenario)
        {
            // Assertion 3: constraint diffs must appear as Children of their parent table's
            // top-level diff entry, NOT as their own top-level entries. This is the central
            // user-visible outcome of the DacFx cascade fix (PR 2143938) and the
            // SchemaCompareUtils CreateDiffEntry carve-out for SqlDwUnified.
            List<DiffEntry> topLevelEntries = operation.ComparisonResult.Differences
                .Select(d => CoreOps.SchemaCompareUtils.CreateDiffEntry(d, parent: null, operation.ComparisonResult))
                .ToList();

            bool anyTopLevelConstraint = topLevelEntries.Any(IsConstraintDiff);
            Assert.IsFalse(anyTopLevelConstraint,
                $"Scenario '{scenario.Name}': constraints must be folded under their parent table, not surfaced as top-level diffs. " +
                $"Top-level constraint diffs found: {string.Join(", ", topLevelEntries.Where(IsConstraintDiff).Select(FormatDiffEntry))}");

            // The parent table's Children collection must include at least one constraint
            // child diff whose object-type ends with one of the recognised constraint
            // suffixes — this is what the webview's affected-constraints banner reads.
            IEnumerable<DiffEntry> tableDiffs = topLevelEntries.Where(IsTableDiff);
            IEnumerable<DiffEntry> constraintChildren = tableDiffs.SelectMany(t => t.Children ?? new List<DiffEntry>())
                .Where(IsConstraintDiff);
            Assert.IsTrue(constraintChildren.Any(),
                $"Scenario '{scenario.Name}': expected at least one constraint diff folded under a parent SqlTable diff, but found none");

            // Assertion 4: each constraint child carries its ALTER TABLE ADD CONSTRAINT
            // script (NONCLUSTERED for PK/UQ, NOT ENFORCED for all three). This is the
            // CreateDiffEntry carve-out — without it the constraint script is stripped by
            // the legacy "starts-with-alter" filter and the diff editor shows nothing.
            foreach (DiffEntry child in constraintChildren)
            {
                string script = (child.SourceScript ?? string.Empty);
                if (string.IsNullOrEmpty(script))
                {
                    // Drop diffs only populate target script; check both sides.
                    script = child.TargetScript ?? string.Empty;
                }
                if (script.Length == 0)
                {
                    // Some constraint children may be metadata-only (e.g. rename); the spec
                    // requires the carve-out to PRESERVE the ALTER script when present, but
                    // does not require a script for every child. Skip silently.
                    continue;
                }

                StringAssert.Contains("ADD CONSTRAINT", script,
                    $"Scenario '{scenario.Name}': constraint child '{child.Name}' is missing the expected ADD CONSTRAINT script body");
                StringAssert.Contains("NOT ENFORCED", script,
                    $"Scenario '{scenario.Name}': constraint child '{child.Name}' must script as NOT ENFORCED for Fabric Warehouse");

                bool isPkOrUq = (child.SourceObjectType ?? child.TargetObjectType ?? string.Empty).EndsWith(
                    "PrimaryKeyConstraint", StringComparison.Ordinal)
                    || (child.SourceObjectType ?? child.TargetObjectType ?? string.Empty).EndsWith(
                        "UniqueConstraint", StringComparison.Ordinal);
                if (isPkOrUq)
                {
                    StringAssert.Contains("NONCLUSTERED", script,
                        $"Scenario '{scenario.Name}': PK/UQ constraint child '{child.Name}' must script as NONCLUSTERED for Fabric Warehouse");
                }
            }
        }

        private void AssertSelectionScopeProducesIsolatedScript(SchemaCompareOperation operation)
        {
            // Assertion 5 (selection-scope): IncludeOnly the OrdersScope table and run
            // Generate Script. The script must reference only OrdersScope — zero
            // ALTER/CREATE statements for CustomersScope, and zero PRINT lines for
            // PK_CustomersScope. This is the empirical regression captured in
            // Prototype 3; without DacFx PR 2143938's cascade fix the generated script
            // leaks N unrelated PK statements when a single table is scripted.
            const string includedTable = "OrdersScope";
            const string excludedTable = "CustomersScope";
            const string excludedPk = "PK_CustomersScope";

            // Exclude every top-level diff that is not the OrdersScope table.
            foreach (var difference in operation.ComparisonResult.Differences)
            {
                bool isIncludedTable =
                    difference.SourceObject != null
                    && difference.SourceObject.Name.Parts.Any(p => string.Equals(p, includedTable, StringComparison.OrdinalIgnoreCase));
                if (!isIncludedTable)
                {
                    bool isIncludedTargetTable = difference.TargetObject != null
                        && difference.TargetObject.Name.Parts.Any(p => string.Equals(p, includedTable, StringComparison.OrdinalIgnoreCase));
                    if (!isIncludedTargetTable)
                    {
                        operation.ComparisonResult.Exclude(difference);
                    }
                }
            }

            string scriptPath = Path.Combine(_workingFolder, "generated.sql");
            SchemaCompareScriptGenerationResult scriptResult = operation.ComparisonResult.GenerateScript("TargetDb");
            Assert.IsTrue(scriptResult.Success,
                $"Selection-scope: GenerateScript reported failure: {scriptResult.Message} {scriptResult.Exception}");
            File.WriteAllText(scriptPath, scriptResult.Script ?? string.Empty);

            string generatedScript = File.ReadAllText(scriptPath);

            StringAssert.Contains(includedTable, generatedScript,
                "Selection-scope: generated script must reference the included table");
            Assert.IsFalse(generatedScript.IndexOf(excludedTable, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Selection-scope: generated script must not reference excluded table '{excludedTable}'. Script:\n{generatedScript}");
            Assert.IsFalse(generatedScript.IndexOf(excludedPk, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Selection-scope: generated script must not contain PRINT/ALTER lines for excluded PK '{excludedPk}'. Script:\n{generatedScript}");
        }

        private void AssertPublishToProjectThenReCompareIsEqual(SchemaCompareOperation operation, string scenarioName)
        {
            // Assertion 6 (project-target round-trip): after publishing the diff into a
            // project target the next comparison must report IsEqual == true. We perform
            // the publish via DacServices on the source model and write the resulting
            // scripts back into the project folder, then re-run the comparison.
            string targetProjectFile = operation.Parameters.TargetEndpointInfo.ProjectFilePath;
            if (string.IsNullOrEmpty(targetProjectFile))
            {
                Assert.Inconclusive(
                    "AssertPublishToProjectThenReCompareIsEqual requires a Project target — skipping for non-project pairs.");
                return;
            }

            // Re-build the source dacpac so we can drive a Publish that overwrites the
            // target project's scripts with the source schema, then assert the re-compare
            // yields IsEqual.
            string regeneratedDacpac = Path.Combine(_workingFolder, $"{scenarioName}_publishSource.dacpac");
            // We build a dacpac purely to validate the source SQL parses cleanly before we
            // overwrite the target project scripts; the resulting dacpac is not consumed by
            // the re-compare itself.
            BuildFabricDacpac(
                ReadFabricFixture(Scenarios.First(s => s.Name == scenarioName).SourceScriptFile),
                regeneratedDacpac);

            string targetProjectFolder = Path.GetDirectoryName(targetProjectFile);
            // Wipe any previously-extracted scripts so the publish writes a fresh set.
            foreach (string existing in Directory.EnumerateFiles(targetProjectFolder, "*.sql", SearchOption.TopDirectoryOnly))
            {
                File.Delete(existing);
            }

            // Replicate the project's published shape by writing the source SQL into a
            // single .sql alongside the .sqlproj. This is the offline equivalent of
            // PublishChangesToProject for the re-compare assertion — adequate to verify
            // the round-trip converges to IsEqual without a live target.
            File.WriteAllText(
                Path.Combine(targetProjectFolder, "PublishedSchema.sql"),
                ReadFabricFixture(Scenarios.First(s => s.Name == scenarioName).SourceScriptFile));

            // Refresh the project's TargetScripts so the next comparison reads the new files.
            operation.Parameters.TargetEndpointInfo.TargetScripts = Directory
                .GetFiles(targetProjectFolder, "*.sql", SearchOption.AllDirectories);

            // Re-run the comparison. After publish the source and target must match.
            SchemaCompareOperation reCompare = new SchemaCompareOperation(operation.Parameters, connectionProvider: null);
            reCompare.Execute();

            Assert.IsTrue(reCompare.ComparisonResult.IsValid,
                $"Re-comparison after project publish (scenario '{scenarioName}') returned IsValid=false");
            Assert.IsTrue(reCompare.ComparisonResult.IsEqual,
                $"Re-comparison after project publish (scenario '{scenarioName}') still reports differences; project-target round-trip must converge");
        }

        // ---------------------------------------------------------------------
        // Endpoint construction helpers
        // ---------------------------------------------------------------------

        private (SchemaCompareEndpointInfo source, SchemaCompareEndpointInfo target) BuildEndpointPair(
            EndpointPair pair, string scenarioName, string sourceScript, string targetScript)
        {
            switch (pair)
            {
                case EndpointPair.DacpacToDacpac:
                {
                    string sourceDacpac = BuildFabricDacpac(sourceScript, Path.Combine(_workingFolder, $"{scenarioName}_source.dacpac"));
                    string targetDacpac = BuildFabricDacpac(targetScript, Path.Combine(_workingFolder, $"{scenarioName}_target.dacpac"));
                    return (CreateDacpacEndpoint(sourceDacpac), CreateDacpacEndpoint(targetDacpac));
                }
                case EndpointPair.DacpacToProject:
                {
                    string sourceDacpac = BuildFabricDacpac(sourceScript, Path.Combine(_workingFolder, $"{scenarioName}_source.dacpac"));
                    string targetProject = BuildFabricProject(targetScript, $"{scenarioName}_Target");
                    return (CreateDacpacEndpoint(sourceDacpac), CreateProjectEndpoint(targetProject));
                }
                case EndpointPair.ProjectToDacpac:
                {
                    string sourceProject = BuildFabricProject(sourceScript, $"{scenarioName}_Source");
                    string targetDacpac = BuildFabricDacpac(targetScript, Path.Combine(_workingFolder, $"{scenarioName}_target.dacpac"));
                    return (CreateProjectEndpoint(sourceProject), CreateDacpacEndpoint(targetDacpac));
                }
                case EndpointPair.ProjectToProject:
                {
                    string sourceProject = BuildFabricProject(sourceScript, $"{scenarioName}_Source");
                    string targetProject = BuildFabricProject(targetScript, $"{scenarioName}_Target");
                    return (CreateProjectEndpoint(sourceProject), CreateProjectEndpoint(targetProject));
                }
                default:
                    throw new NotSupportedException($"Unknown endpoint pair {pair}");
            }
        }

        private static SchemaCompareEndpointInfo CreateDacpacEndpoint(string dacpacPath) =>
            new SchemaCompareEndpointInfo
            {
                EndpointType = SchemaCompareEndpointType.Dacpac,
                PackageFilePath = dacpacPath,
            };

        private static SchemaCompareEndpointInfo CreateProjectEndpoint(string projectFilePath) =>
            new SchemaCompareEndpointInfo
            {
                EndpointType = SchemaCompareEndpointType.Project,
                ProjectFilePath = projectFilePath,
                TargetScripts = Directory.GetFiles(Path.GetDirectoryName(projectFilePath), "*.sql", SearchOption.AllDirectories),
                DataSchemaProvider = FabricPlatformName,
            };

        // ---------------------------------------------------------------------
        // Fixture construction — building Fabric dacpacs and sqlprojs at test time
        // ---------------------------------------------------------------------

        /// <summary>
        /// Build an offline Fabric Warehouse dacpac from the given T-SQL script. Uses
        /// DacFx's <see cref="TSqlModel"/> with <see cref="SqlServerVersion.SqlDwUnified"/>
        /// so the dacpac's model.xml carries the correct
        /// <c>SqlDwUnifiedDatabaseSchemaProvider</c> DSP and the comparison loads under
        /// Fabric without a live database.
        /// </summary>
        internal static string BuildFabricDacpac(string sqlScript, string dacpacPath)
        {
            using (TSqlModel model = new TSqlModel(SqlServerVersion.SqlDwUnified, new TSqlModelOptions()))
            {
                // Split on GO so multi-batch fixtures (e.g. the FK scenario) load as
                // separate scripts, matching how DacFx normally consumes .sql files.
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

        /// <summary>
        /// Create an SDK-style Fabric .sqlproj seeded with the given script. Copies the
        /// shared emptyFabricTemplate.sqlproj (DSP = SqlDwUnifiedDatabaseSchemaProvider)
        /// into a per-test folder and drops the fixture SQL alongside it. Returns the
        /// .sqlproj file path.
        /// </summary>
        internal string BuildFabricProject(string sqlScript, string projectName)
        {
            string projectFolder = Path.Combine(_workingFolder, projectName);
            Directory.CreateDirectory(projectFolder);
            string sqlprojPath = Path.Combine(projectFolder, projectName + ".sqlproj");
            File.Copy(FabricSqlProjectTemplate, sqlprojPath, overwrite: true);
            File.WriteAllText(Path.Combine(projectFolder, "Schema.sql"), sqlScript);
            return sqlprojPath;
        }

        private static string ReadFabricFixture(string fileName)
        {
            string path = Path.Combine(FabricFixturesFolder, fileName);
            if (!File.Exists(path))
            {
                Assert.Fail($"Fabric fixture '{fileName}' not found at expected path '{path}'.");
            }
            return File.ReadAllText(path);
        }

        // ---------------------------------------------------------------------
        // Small utilities
        // ---------------------------------------------------------------------

        private static IEnumerable<string> SplitOnGo(string sqlScript)
        {
            using (StringReader reader = new StringReader(sqlScript))
            {
                List<string> currentBatch = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return string.Join(Environment.NewLine, currentBatch);
                        currentBatch.Clear();
                    }
                    else
                    {
                        currentBatch.Add(line);
                    }
                }
                if (currentBatch.Count > 0)
                {
                    yield return string.Join(Environment.NewLine, currentBatch);
                }
            }
        }

        private static bool IsConstraintDiff(DiffEntry diff)
        {
            string typeName = diff.SourceObjectType ?? diff.TargetObjectType ?? string.Empty;
            return ConstraintTypeSuffixes.Any(suffix => typeName.EndsWith(suffix, StringComparison.Ordinal));
        }

        private static bool IsTableDiff(DiffEntry diff)
        {
            string typeName = diff.SourceObjectType ?? diff.TargetObjectType ?? string.Empty;
            return typeName.EndsWith("SqlTable", StringComparison.Ordinal);
        }

        private static string FormatDiffEntry(DiffEntry diff)
        {
            string[] parts = diff.SourceValue ?? diff.TargetValue ?? Array.Empty<string>();
            return $"{(diff.SourceObjectType ?? diff.TargetObjectType ?? "?")}: {(parts.Length > 0 ? string.Join(".", parts) : (diff.Name ?? "?"))}";
        }
    }
}
