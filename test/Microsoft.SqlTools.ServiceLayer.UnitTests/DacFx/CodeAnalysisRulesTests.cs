//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DacFx
{
    [TestFixture]
    public class CodeAnalysisRulesTests
    {
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
                    System.Enum.IsDefined(typeof(SqlRuleProblemSeverity), rule.Severity),
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
        public async Task SqlProjectProperties_SetAndDeleteProperty_UpdatesCodeAnalysisProperties()
        {
            var path = await CreateTestSqlprojAsync();
            try
            {
                XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

                // Set properties and verify
                SqlProject project = SqlProject.OpenProject(path, onlyLoadProperties: true);
                project.Properties.SetProperty("SqlCodeAnalysisRules", "+!SR0001;-SR0005");
                project.Properties.SetProperty("RunSqlCodeAnalysis", "True");

                var doc = XDocument.Load(path);
                Assert.That(doc.Descendants(ns + "SqlCodeAnalysisRules").FirstOrDefault()?.Value, Is.EqualTo("+!SR0001;-SR0005"));
                Assert.That(doc.Descendants(ns + "RunSqlCodeAnalysis").FirstOrDefault()?.Value, Is.EqualTo("True"));

                // Delete property and verify it is removed
                project = SqlProject.OpenProject(path, onlyLoadProperties: true);
                project.Properties.DeleteProperty("SqlCodeAnalysisRules");

                doc = XDocument.Load(path);
                Assert.That(doc.Descendants(ns + "SqlCodeAnalysisRules").FirstOrDefault(), Is.Null);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static async Task<string> CreateTestSqlprojAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{TestContext.CurrentContext.Test.Name}_{System.Guid.NewGuid()}.sqlproj");

            await SqlProject.CreateProjectAsync(path, new Microsoft.SqlServer.Dac.Projects.CreateSqlProjectParams
            {
                ProjectType = ProjectType.LegacyStyle
            });

            return path;
        }
    }
}
