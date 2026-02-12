//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Linq;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Model;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DacFx
{
    [TestFixture]
    public class CodeAnalysisRulesTests
    {
        /// <summary>
        /// Verify that DacFx CodeAnalysisService returns exactly 14 built-in rules
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesReturnsAll14Rules()
        {
            // Arrange - Create minimal model
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert
            Assert.AreEqual(14, rules.Count, "DacFx should provide exactly 14 built-in code analysis rules");
        }

        /// <summary>
        /// Verify that each rule has required properties populated
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesReturnsValidRuleProperties()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert
            foreach (var rule in rules)
            {
                Assert.IsNotNull(rule.RuleId, "RuleId should not be null");
                Assert.IsNotEmpty(rule.RuleId, "RuleId should not be empty");
                Assert.IsNotNull(rule.DisplayName, $"DisplayName should not be null for {rule.RuleId}");
                Assert.IsNotNull(rule.Severity, $"Severity should not be null for {rule.RuleId}");
            }
        }

        /// <summary>
        /// Verify that known rule IDs are present
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesContainsKnownRuleIds()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();
            var ruleIds = rules.Select(r => r.RuleId).ToList();

            // Assert - Verify some known rule IDs exist
            Assert.IsTrue(ruleIds.Any(id => id.Contains("SR0001")), "Should contain SR0001");
            Assert.IsTrue(ruleIds.Any(id => id.Contains("SR0004")), "Should contain SR0004");
            Assert.IsTrue(ruleIds.Any(id => id.Contains("SR0008")), "Should contain SR0008");
        }

        /// <summary>
        /// Verify rule metadata contains category and scope information
        /// </summary>
        [Test]
        public void GetCodeAnalysisRulesContainsMetadata()
        {
            // Arrange
            using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            var factory = new CodeAnalysisServiceFactory();
            var codeAnalysisService = factory.CreateAnalysisService(model);

            // Act
            var rules = codeAnalysisService.GetRules().ToList();

            // Assert - At least some rules should have metadata
            var rulesWithCategory = rules.Where(r => r.Metadata?.Category != null).ToList();
            Assert.IsTrue(rulesWithCategory.Count > 0, "At least some rules should have a category");
        }
    }
}
