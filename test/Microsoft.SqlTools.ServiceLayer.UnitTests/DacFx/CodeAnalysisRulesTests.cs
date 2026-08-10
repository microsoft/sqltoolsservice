//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DacFx
{
    [TestFixture]
    public class CodeAnalysisRulesTests
    {
        private const string PackageId = "Contoso.SqlRules";
        private const string PackageVersion = "1.2.3";
        private const string AnalyzerRelativePath = "analyzers/dotnet/cs/Contoso.SqlRules.dll";
        private const string LibRelativePath = "lib/netstandard2.1/Contoso.SqlRules.dll";
        private const string NuGetPackagesVariable = "NUGET_PACKAGES";

        private string testRoot;
        private string projectDirectory;
        private string projectFilePath;
        private string packagesDirectory;
        private string emptyPackagesDirectory;

        /// <summary>
        /// Builds a throwaway project folder plus a fake NuGet package cache for the custom rule tests.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            testRoot = Path.Combine(Path.GetTempPath(), nameof(CodeAnalysisRulesTests), Guid.NewGuid().ToString("N"));
            projectDirectory = Path.Combine(testRoot, "project");
            projectFilePath = Path.Combine(projectDirectory, "TestProject.sqlproj");
            packagesDirectory = Path.Combine(testRoot, "packages");
            emptyPackagesDirectory = Path.Combine(testRoot, "empty-packages");

            Directory.CreateDirectory(projectDirectory);
            Directory.CreateDirectory(packagesDirectory);
            Directory.CreateDirectory(emptyPackagesDirectory);
            File.WriteAllText(projectFilePath, "<Project />");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, recursive: true);
            }
        }

        /// <summary>
        /// Verify that DacFx CodeAnalysisService returns at least one built-in rule
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesReturnsAtLeastOneRule()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql170, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert
            Assert.GreaterOrEqual(rules.Count, 1, "DacFx should provide at least one built-in code analysis rule");
        }

        /// <summary>
        /// Verify that each rule has required properties populated
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesReturnsValidRuleProperties()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql170, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert - every rule must have its key properties populated with meaningful values
            foreach (var rule in rules)
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(rule.RuleId),
                    "RuleId should not be null, empty, or whitespace"
                );
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(rule.ShortRuleId),
                    $"ShortRuleId should not be null, empty, or whitespace for {rule.RuleId}"
                );
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(rule.DisplayName),
                    $"DisplayName should not be null, empty, or whitespace for {rule.RuleId}"
                );
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(rule.DisplayDescription),
                    $"DisplayDescription should not be null, empty, or whitespace for {rule.RuleId}"
                );
                Assert.IsTrue(
                    System.Enum.IsDefined(rule.Severity),
                    $"Severity should be a defined {nameof(SqlRuleProblemSeverity)} value for {rule.RuleId}"
                );
            }
        }

        /// <summary>
        /// Verify that rules have metadata with category and scope information
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesContainsMetadata()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql170, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert
            var rulesWithCategory = rules.Where(r => r.Metadata?.Category != null).ToList();
            Assert.IsTrue(rulesWithCategory.Count > 0, "At least some rules should have a category");

            var rulesWithScope = rules.Where(r => r.Metadata?.RuleScope != null).ToList();
            Assert.IsTrue(rulesWithScope.Count > 0, "At least some rules should have a rule scope");
        }

        [Test]
        public void BuildCodeAnalysisRulesXmlValue_MixedRules_SerializesExpectedTokens()
        {
            var rules = new List<CodeAnalysisRuleOverride>
            {
                new() { RuleId = "SR0001", Severity = "Error" },
                new() { RuleId = "SR0002", Severity = "Warning" }, // omitted
                new() { RuleId = "SR0003", Severity = "Disabled" },
            };

            var result = SqlProjectsService.BuildCodeAnalysisRulesXmlValue(rules);

            // Newer DacFx settings serialization uses +!<RuleId> for Error overrides.
            Assert.That(result, Is.EqualTo("+!SR0001;-SR0003"));
        }

        [Test]
        public void BuildCodeAnalysisRulesXmlValue_EmptyRules_ReturnsEmpty()
        {
            var result = SqlProjectsService.BuildCodeAnalysisRulesXmlValue(new List<CodeAnalysisRuleOverride>());
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void BuildCodeAnalysisRulesXmlValue_UnknownOrNullSeverity_SkipsEntry()
        {
            // Unrecognized severities (including null) are treated as "use DacFx default" and
            // produce no override entry, matching the Warning / default-severity behaviour.
            var rules = new List<CodeAnalysisRuleOverride>
            {
                new() { RuleId = "SR0001", Severity = "Errror" },  // typo → skipped
                new() { RuleId = "SR0002", Severity = null },       // null  → skipped
                new() { RuleId = "SR0003", Severity = "Error" },    // valid → included
            };

            var result = SqlProjectsService.BuildCodeAnalysisRulesXmlValue(rules);
            Assert.That(result, Is.EqualTo("+!SR0003"));
        }

        [Test]
        public void BuildCodeAnalysisRulesXmlValue_EmptyRuleId_SkipsEntry()
        {
            var rules = new List<CodeAnalysisRuleOverride>
            {
                new() { RuleId = "  ", Severity = "Error" },   // blank RuleId → skipped
                new() { RuleId = "SR0001", Severity = "Error" },
            };

            var result = SqlProjectsService.BuildCodeAnalysisRulesXmlValue(rules);
            Assert.That(result, Is.EqualTo("+!SR0001"));
        }

        #region Finding custom rule assemblies from package references

        [Test]
        public void ResolveAnalyzerAssemblyPaths_PackagedAssembly_IsResolvedFromTheContainingPackageFolder()
        {
            string expectedPath = WriteFileIntoPackage(AnalyzerRelativePath);
            string assetsFilePath = WriteAssetsFileFor(AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.EqualTo(new[] { expectedPath }));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_FilesOutsideTheAnalyzerFolder_AreIgnored()
        {
            // The build only loads assemblies NuGet resolves into @(Analyzer), which is the
            // analyzers/dotnet/cs folder. Rules shipped anywhere else never run.
            string assetsFilePath = WriteAssetsFileFor(
                "readme.md",                                 // not an assembly
                "analyzers/dotnet/cs/Contoso.SqlRules.xml",  // not an assembly
                LibRelativePath,                             // lib/ is not discovered by the build
                "Contoso.SqlRules.dll",                      // package root
                "tools/Contoso.SqlRules.dll");               // tools/

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.Empty);
            Assert.That(result.Warnings, Is.Empty, "Ignored files should not be reported as missing assemblies");
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_LibrariesTheProjectDoesNotDirectlyReference_AreIgnored()
        {
            // Transitive dependencies and project references must not be loaded and reflected over.
            WriteFileIntoPackage(AnalyzerRelativePath, packageId: "Transitive.Package");
            WriteFileIntoPackage(AnalyzerRelativePath);

            string assetsFilePath = WriteRawAssetsFile(BuildAssetsJson(
                packageFolders: new[] { packagesDirectory },
                directDependencies: new[] { PackageId },
                libraries: new[]
                {
                    new LibraryEntry("Transitive.Package", "package", new[] { AnalyzerRelativePath }),
                    new LibraryEntry(PackageId, "project", new[] { AnalyzerRelativePath }),
                }));

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.Empty);
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_AssemblyMissingFromPackageFolders_AddsWarning()
        {
            // The assets file lists the assembly but nothing was written to the package cache.
            string assetsFilePath = WriteAssetsFileFor(AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("Contoso.SqlRules.dll"));
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_NoPackageFoldersRecorded_FallsBackToNuGetPackagesVariable()
        {
            string originalValue = Environment.GetEnvironmentVariable(NuGetPackagesVariable);

            try
            {
                Environment.SetEnvironmentVariable(NuGetPackagesVariable, packagesDirectory);
                string expectedPath = WriteFileIntoPackage(AnalyzerRelativePath);

                string assetsFilePath = WriteRawAssetsFile(BuildAssetsJson(
                    packageFolders: Array.Empty<string>(),
                    directDependencies: new[] { PackageId },
                    libraries: new[] { new LibraryEntry(PackageId, "package", new[] { AnalyzerRelativePath }) }));

                CustomRuleLoader.LoadResult result = new();
                List<string> assemblyPaths = ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

                Assert.That(assemblyPaths, Is.EqualTo(new[] { expectedPath }));
            }
            finally
            {
                Environment.SetEnvironmentVariable(NuGetPackagesVariable, originalValue);
            }
        }

        #endregion

        #region Loading custom rules for a project

        [Test]
        public void LoadFromProject_AssetsFileIsNotValidJson_ReportsLoadFailure()
        {
            WriteRawAssetsFile("this is not json");

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.Rules, Is.Empty);
            Assert.That(result.Warning, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void LoadFromProject_AssemblyCannotBeLoaded_ReportsTheAssembly()
        {
            WriteFileIntoPackage(AnalyzerRelativePath, contents: "not a real assembly");
            WriteAssetsFileFor(AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.Rules, Is.Empty);
            Assert.That(result.Warning, Does.Contain("Contoso.SqlRules.dll"));
        }

        [Test]
        public void LoadFromProject_ProjectGivenAsFileUri_ResolvesTheSameProject()
        {
            // LSP clients send file:// URIs, so both forms must locate obj/project.assets.json.
            WriteAssetsFileFor(AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result =
                CustomRuleLoader.LoadFromProject(new Uri(projectFilePath).AbsoluteUri);

            Assert.That(result.Warning, Does.Contain("Contoso.SqlRules.dll"),
                "A file:// URI should find the assets file rather than report that restore is required");
        }

        [Test]
        public void LoadResult_RuleIdContributedTwice_IsKeptOnceAndWarned()
        {
            // Two analyzer packages can declare the same rule ID.
            CustomRuleLoader.LoadResult result = new();
            result.AddRule(new CodeAnalysisRuleInfo { RuleId = "Contoso.SR1000" });
            result.AddRule(new CodeAnalysisRuleInfo { RuleId = "contoso.sr1000" });

            Assert.That(result.Rules, Has.Count.EqualTo(1));
            Assert.That(result.Warning, Does.Contain("Contoso.SR1000").IgnoreCase);
        }

        #endregion

        #region Merging built-in and custom rules

        [TestCase(null, Description = "No project supplied")]
        [TestCase("   ", Description = "Blank project path")]
        public void GetCodeAnalysisRules_WithoutProjectPath_ReturnsOnlyBuiltInRulesAndNoWarning(string projectPath)
        {
            GetCodeAnalysisRulesResult result = DacFxService.GetCodeAnalysisRules(projectPath);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Rules, Is.Not.Empty);
            Assert.That(result.Rules.Select(rule => rule.IsBuiltIn), Is.All.True);
            Assert.That(result.Warning, Is.Null);
        }

        [Test]
        public void GetCodeAnalysisRules_UnrestoredProject_StillReturnsBuiltInRulesWithAWarning()
        {
            GetCodeAnalysisRulesResult result = DacFxService.GetCodeAnalysisRules(projectFilePath);

            Assert.That(result.Rules, Is.Not.Empty, "Built-in rules must still be returned when custom rules are unavailable");
            Assert.That(result.Warning, Is.EqualTo(SR.CustomRulesRestoreRequired));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Writes an assets file describing one directly referenced analyzer package that contains
        /// <paramref name="packageFiles"/>. An empty package folder is listed first so that probing
        /// across several package folders is exercised.
        /// </summary>
        private string WriteAssetsFileFor(params string[] packageFiles) => WriteRawAssetsFile(BuildAssetsJson(
            packageFolders: new[] { emptyPackagesDirectory, packagesDirectory },
            directDependencies: new[] { PackageId },
            libraries: new[] { new LibraryEntry(PackageId, "package", packageFiles) }));

        private string WriteRawAssetsFile(string contents)
        {
            string objDirectory = Path.Combine(projectDirectory, "obj");
            Directory.CreateDirectory(objDirectory);

            string assetsFilePath = Path.Combine(objDirectory, "project.assets.json");
            File.WriteAllText(assetsFilePath, contents);
            return assetsFilePath;
        }

        /// <summary>
        /// Places a file in the fake package cache, laid out the way NuGet does:
        /// &lt;packageFolder&gt;/&lt;lowercase id&gt;/&lt;version&gt;/&lt;file&gt;.
        /// </summary>
        private string WriteFileIntoPackage(string packageRelativePath, string packageId = PackageId, string contents = "")
        {
            string destination = Path.Combine(
                packagesDirectory,
                packageId.ToLowerInvariant(),
                PackageVersion,
                packageRelativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, contents);
            return destination;
        }

        private sealed record LibraryEntry(string Id, string Type, string[] Files);

        private static string BuildAssetsJson(
            string[] packageFolders,
            string[] directDependencies,
            LibraryEntry[] libraries)
        {
            Dictionary<string, object> root = new()
            {
                ["version"] = 3,
                ["libraries"] = libraries.ToDictionary(
                    library => $"{library.Id}/{PackageVersion}",
                    library => (object)new Dictionary<string, object>
                    {
                        ["type"] = library.Type,
                        ["files"] = library.Files,
                    }),
                ["packageFolders"] = packageFolders.ToDictionary(
                    folder => folder,
                    _ => (object)new Dictionary<string, object>()),
                ["project"] = new Dictionary<string, object>
                {
                    ["frameworks"] = new Dictionary<string, object>
                    {
                        ["net8.0"] = new Dictionary<string, object>
                        {
                            ["dependencies"] = directDependencies.ToDictionary(
                                dependency => dependency,
                                _ => (object)new Dictionary<string, string>
                                {
                                    ["target"] = "Package",
                                    ["version"] = "[1.0.0, )",
                                }),
                        },
                    },
                },
            };

            return JsonSerializer.Serialize(root);
        }

        /// <summary>
        /// Parses an assets file and resolves the analyzer assemblies it records. The loader takes the
        /// parsed document so the file is only read once per load; the tests start from a path.
        /// </summary>
        private static List<string> ResolveAnalyzerAssemblyPaths(string assetsFilePath, CustomRuleLoader.LoadResult result)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsFilePath));
            return CustomRuleLoader.ResolveAnalyzerAssemblyPaths(document.RootElement, result);
        }

        /// <summary>
        /// Writes a project referencing <paramref name="packageId"/> at <paramref name="version"/>.
        /// </summary>
        /// <param name="useChildElement">
        /// When true the version is written as a child element rather than an attribute, which MSBuild
        /// allows for either form.
        /// </param>
        private void WriteProjectReferencing(string packageId, string version, bool useChildElement = false)
        {
            string reference = useChildElement
                ? $"<PackageReference Include=\"{packageId}\"><Version>{version}</Version></PackageReference>"
                : $"<PackageReference Include=\"{packageId}\" Version=\"{version}\" />";

            File.WriteAllText(projectFilePath, $"<Project><ItemGroup>{reference}</ItemGroup></Project>");
        }

        /// <summary>
        /// Writes an assets file recording <paramref name="packageId"/> as restored at
        /// <paramref name="restoredVersion"/>, containing <paramref name="packageFiles"/>.
        /// </summary>
        private void WriteAssetsFileWithRestoredVersion(string packageId, string restoredVersion, params string[] packageFiles)
        {
            Dictionary<string, object> root = new()
            {
                ["version"] = 3,
                ["libraries"] = new Dictionary<string, object>
                {
                    [$"{packageId}/{restoredVersion}"] = new Dictionary<string, object>
                    {
                        ["type"] = "package",
                        ["files"] = packageFiles,
                    },
                },
                ["packageFolders"] = new Dictionary<string, object> { [packagesDirectory] = new Dictionary<string, object>() },
                ["project"] = new Dictionary<string, object>
                {
                    ["frameworks"] = new Dictionary<string, object>
                    {
                        ["net8.0"] = new Dictionary<string, object>
                        {
                            ["dependencies"] = new Dictionary<string, object>
                            {
                                [packageId] = new Dictionary<string, string> { ["target"] = "Package", ["version"] = "[1.0.0, )" },
                            },
                        },
                    },
                },
            };

            WriteRawAssetsFile(JsonSerializer.Serialize(root));
        }

        #endregion

        #region Restore required detection

        /// <summary>
        /// The assets file only records the last successful restore, so a version bump that has not
        /// been restored has to be reported rather than silently loading the old rules.
        /// </summary>
        [Test]
        public void LoadFromProject_AnalyzerPackageVersionDiffersFromRestored_ReportsRestoreRequired()
        {
            WriteProjectReferencing(PackageId, "2.0.0");
            WriteAssetsFileWithRestoredVersion(PackageId, "1.0.0", AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.True, "an unrestored version bump should require a restore");
            Assert.That(result.Warning, Does.Contain("2.0.0").And.Contain("1.0.0"),
                "the warning should name both the referenced and the restored version");
        }

        [Test]
        public void LoadFromProject_AnalyzerPackageVersionMatchesRestored_ReportsNoRestoreRequired()
        {
            WriteProjectReferencing(PackageId, PackageVersion);
            WriteAssetsFileWithRestoredVersion(PackageId, PackageVersion, AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.False, "a matching version needs no restore");
        }

        /// <summary>
        /// Only analyzer packages contribute rules, so an unrelated package left behind by a partial
        /// restore must not raise a code analysis warning.
        /// </summary>
        [Test]
        public void LoadFromProject_NonAnalyzerPackageVersionDiffersFromRestored_IsIgnored()
        {
            WriteProjectReferencing(PackageId, "2.0.0");
            WriteAssetsFileWithRestoredVersion(PackageId, "1.0.0", LibRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.False, "a package that ships no analyzers is not a source of rules");
            Assert.That(result.Warning, Is.Null);
        }

        /// <summary>
        /// Without a restore there is no file list for the package, so whether it contributes rules is
        /// unknowable. Reporting it anyway would flag unrelated packages as code analysis problems.
        /// </summary>
        [Test]
        public void LoadFromProject_ReferencedPackageMissingFromAssets_IsIgnored()
        {
            WriteProjectReferencing("Contoso.NotRestored", "1.0.0");
            WriteAssetsFileWithRestoredVersion(PackageId, PackageVersion, AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.False, "a package with no restored file list cannot be classified");
        }

        /// <summary>
        /// A range or floating version can resolve to many versions, so comparing it against a single
        /// restored version by string equality would report a mismatch that does not exist.
        /// </summary>
        [TestCase("2.*")]
        [TestCase("[1.0.0, 2.0.0)")]
        public void LoadFromProject_ReferencedVersionIsNotPinned_IsIgnored(string referencedVersion)
        {
            WriteProjectReferencing(PackageId, referencedVersion);
            WriteAssetsFileWithRestoredVersion(PackageId, "1.5.0", AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.False, $"'{referencedVersion}' cannot be compared by string equality");
        }

        [Test]
        public void LoadFromProject_VersionGivenAsChildElement_IsComparedTheSameAsAnAttribute()
        {
            WriteProjectReferencing(PackageId, "2.0.0", useChildElement: true);
            WriteAssetsFileWithRestoredVersion(PackageId, "1.0.0", AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.True, "the version element form should be read like the attribute form");
        }

        [Test]
        public void LoadFromProject_ProjectNeverRestored_ReportsRestoreRequired()
        {
            WriteProjectReferencing(PackageId, PackageVersion);

            CustomRuleLoader.LoadResult result = CustomRuleLoader.LoadFromProject(projectFilePath);

            Assert.That(result.RestoreRequired, Is.True, "a project with no assets file has never been restored");
        }

        #endregion
    }
}
