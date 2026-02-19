//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

            // Assert - every rule must have its key properties populated
            foreach (var rule in rules)
            {
                Assert.IsNotNull(rule.RuleId, "RuleId should not be null");
                Assert.IsNotNull(rule.ShortRuleId, $"ShortRuleId should not be null for {rule.RuleId}");
                Assert.IsNotNull(rule.DisplayName, $"DisplayName should not be null for {rule.RuleId}");
                Assert.IsNotNull(rule.DisplayDescription, $"DisplayDescription should not be null for {rule.RuleId}");
                Assert.IsNotNull(rule.Severity, $"Severity should not be null for {rule.RuleId}");
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
    }
}
