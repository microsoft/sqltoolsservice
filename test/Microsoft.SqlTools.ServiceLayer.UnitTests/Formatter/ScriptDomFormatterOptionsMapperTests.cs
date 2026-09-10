//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.LanguageService.Formatter;
using Microsoft.SqlTools.LanguageService.Formatter.Contracts;
using Microsoft.SqlTools.LanguageService.Formatter.ScriptDom;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class ScriptDomFormatterOptionsMapperTests
    {
        [Test]
        public void ResolveShouldMapFullCanonicalSettingsSurface()
        {
            SqlFormatterOptions formatterOptions = CreateNonDefaultFormatterOptions();

            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(null, formatterOptions);

            foreach (var property in typeof(SqlFormatterOptions).GetProperties())
            {
                var effectiveProperty = typeof(ScriptDomFormatterSettings).GetProperty(property.Name);
                Assert.NotNull(effectiveProperty, property.Name);
                object expected = property.GetValue(formatterOptions);
                object actual = effectiveProperty.GetValue(settings);
                if (property.PropertyType.IsEnum)
                {
                    Assert.AreEqual(expected.ToString(), actual.ToString(), property.Name);
                }
                else
                {
                    Assert.AreEqual(expected, actual, property.Name);
                }
            }
        }

        [Test]
        public void ToScriptGeneratorOptionsShouldMapFullSettingsSurface()
        {
            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(
                new FormattingOptions { InsertSpaces = false, TabSize = 2 },
                CreateNonDefaultFormatterOptions());

            var options = ScriptDomFormatterOptionsMapper.ToScriptGeneratorOptions(settings);

            foreach (var property in typeof(ScriptDomFormatterSettings).GetProperties())
            {
                var generatorProperty = options.GetType().GetProperty(property.Name);
                Assert.NotNull(generatorProperty, property.Name);
                Assert.AreEqual(property.GetValue(settings), generatorProperty.GetValue(options), property.Name);
            }
        }

        [TestCase(SqlFormatterVersion.Sql180, "Sql180")]
        [TestCase(SqlFormatterVersion.SqlFabricDW, "SqlFabricDW")]
        public void ResolveShouldMapLatestSqlVersions(SqlFormatterVersion sqlVersion, string expected)
        {
            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(
                null,
                new SqlFormatterOptions { SqlVersion = sqlVersion });

            Assert.AreEqual(expected, settings.SqlVersion.ToString());
        }

        [Test]
        public void ResolveShouldOverlayCanonicalOptionsAndLspIndentation()
        {
            SqlFormatterOptions formatterOptions = new SqlFormatterOptions
            {
                AlignColumnDefinitionFields = false,
                KeywordCasing = SqlFormatterKeywordCasing.Lowercase,
                NewLineBeforeFromClause = false,
                NumNewlinesAfterStatement = 3
            };

            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(
                new FormattingOptions { InsertSpaces = true, TabSize = 2 },
                formatterOptions);

            Assert.False(settings.AlignColumnDefinitionFields);
            Assert.AreEqual("Lowercase", settings.KeywordCasing.ToString());
            Assert.False(settings.NewLineBeforeFromClause);
            Assert.True(settings.NewLineBeforeOrderByClause);
            Assert.True(settings.NewLineBeforeWhereClause);
            Assert.AreEqual(3, settings.NumNewlinesAfterStatement);
            Assert.AreEqual(2, settings.IndentationSize);
            Assert.AreEqual("Spaces", settings.IndentationMode.ToString());
            Assert.True(settings.PreserveComments);
        }

        [Test]
        public void ResolveShouldUseTabsWhenRequestedByLsp()
        {
            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(
                new FormattingOptions { InsertSpaces = false, TabSize = 4 },
                null);

            Assert.AreEqual("Tabs", settings.IndentationMode.ToString());
            Assert.AreEqual(4, settings.IndentationSize);
        }

        [Test]
        public void ResolveShouldRetainDefaultsForInvalidValues()
        {
            SqlFormatterOptions formatterOptions = new SqlFormatterOptions
            {
                SqlVersion = (SqlFormatterVersion)(-1),
                SqlEngineType = (SqlFormatterEngineType)(-1),
                BuiltInFunctionCasing = (SqlFormatterBuiltInFunctionCasing)(-1),
                ClauseBodyAlignment = (SqlFormatterClauseBodyAlignment)(-1),
                ColumnAliasStyle = (SqlFormatterColumnAliasStyle)(-1),
                CommaPlacement = (SqlFormatterCommaPlacement)(-1),
                IdentifierBracketing = (SqlFormatterIdentifierBracketing)(-1),
                IdentifierCasing = (SqlFormatterIdentifierCasing)(-1),
                KeywordCasing = (SqlFormatterKeywordCasing)(-1),
                LeadingCommaSpaceCount = 2,
                NumNewlinesAfterBatches = 6,
                NumNewlinesAfterBatchStatement = -1,
                NumNewlinesAfterStatement = 6
            };

            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(null, formatterOptions);

            Assert.AreEqual("Sql170", settings.SqlVersion.ToString());
            Assert.AreEqual("All", settings.SqlEngineType.ToString());
            Assert.AreEqual("Preserve", settings.BuiltInFunctionCasing.ToString());
            Assert.AreEqual("Aligned", settings.ClauseBodyAlignment.ToString());
            Assert.AreEqual("AsKeyword", settings.ColumnAliasStyle.ToString());
            Assert.AreEqual("Trailing", settings.CommaPlacement.ToString());
            Assert.AreEqual("Preserve", settings.IdentifierBracketing.ToString());
            Assert.AreEqual("Preserve", settings.IdentifierCasing.ToString());
            Assert.AreEqual("Uppercase", settings.KeywordCasing.ToString());
            Assert.AreEqual(1, settings.LeadingCommaSpaceCount);
            Assert.AreEqual(1, settings.NumNewlinesAfterBatches);
            Assert.AreEqual(2, settings.NumNewlinesAfterBatchStatement);
            Assert.AreEqual(1, settings.NumNewlinesAfterStatement);
            Assert.AreEqual(4, settings.IndentationSize);
        }

        private static SqlFormatterOptions CreateNonDefaultFormatterOptions()
        {
            SqlFormatterOptions formatterOptions = new SqlFormatterOptions
            {
                SqlVersion = SqlFormatterVersion.Sql180,
                SqlEngineType = SqlFormatterEngineType.Standalone,
                BuiltInFunctionCasing = SqlFormatterBuiltInFunctionCasing.Lowercase,
                ClauseBodyAlignment = SqlFormatterClauseBodyAlignment.Indented,
                ColumnAliasStyle = SqlFormatterColumnAliasStyle.EqualsSign,
                CommaPlacement = SqlFormatterCommaPlacement.Leading,
                IdentifierBracketing = SqlFormatterIdentifierBracketing.ExcludeBrackets,
                IdentifierCasing = SqlFormatterIdentifierCasing.PascalCase,
                KeywordCasing = SqlFormatterKeywordCasing.PascalCase,
                LeadingCommaSpaceCount = 0,
                NumNewlinesAfterBatches = 3,
                NumNewlinesAfterBatchStatement = 4,
                NumNewlinesAfterStatement = 3
            };
            foreach (var property in typeof(SqlFormatterOptions).GetProperties())
            {
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(formatterOptions, !(bool)property.GetValue(formatterOptions));
                }
            }

            return formatterOptions;
        }
    }
}
