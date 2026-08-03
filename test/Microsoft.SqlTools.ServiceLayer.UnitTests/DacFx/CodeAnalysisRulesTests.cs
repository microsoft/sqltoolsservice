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

        [TestCase(AnalyzerRelativePath, Description = "analyzers/dotnet/cs layout, e.g. ErikEJ.DacFX.SqlServer.Rules")]
        [TestCase(LibRelativePath, Description = "lib layout, e.g. a class library packed as-is")]
        public void ResolveAnalyzerAssemblyPaths_PackagedAssembly_IsResolvedFromTheContainingPackageFolder(string packageRelativePath)
        {
            string expectedPath = WriteFileIntoPackage(packageRelativePath);
            string assetsFilePath = WriteAssetsFileFor(packageRelativePath);

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = CustomRuleLoader.ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.EqualTo(new[] { expectedPath }));
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_FilesOutsideAnalyzerAndLibFolders_AreIgnored()
        {
            string assetsFilePath = WriteAssetsFileFor(
                "readme.md",                                 // not an assembly
                "analyzers/dotnet/cs/Contoso.SqlRules.xml",  // not an assembly
                "Contoso.SqlRules.dll",                      // outside analyzers/ and lib/
                "tools/Contoso.SqlRules.dll");               // outside analyzers/ and lib/

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = CustomRuleLoader.ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

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
            List<string> assemblyPaths = CustomRuleLoader.ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

            Assert.That(assemblyPaths, Is.Empty);
        }

        [Test]
        public void ResolveAnalyzerAssemblyPaths_AssemblyMissingFromPackageFolders_AddsWarning()
        {
            // The assets file lists the assembly but nothing was written to the package cache.
            string assetsFilePath = WriteAssetsFileFor(AnalyzerRelativePath);

            CustomRuleLoader.LoadResult result = new();
            List<string> assemblyPaths = CustomRuleLoader.ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

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
                List<string> assemblyPaths = CustomRuleLoader.ResolveAnalyzerAssemblyPaths(assetsFilePath, result);

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

        #endregion
    }
}
