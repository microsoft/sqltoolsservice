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
            SqlFormatterOptions formatterOptions = new SqlFormatterOptions
            {
                SqlVersion = SqlFormatterVersion.Sql160,
                SqlEngineType = SqlFormatterEngineType.Standalone,
                KeywordCasing = SqlFormatterKeywordCasing.PascalCase,
                NumNewlinesAfterStatement = 3
            };
            foreach (var property in typeof(SqlFormatterOptions).GetProperties())
            {
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(formatterOptions, !(bool)property.GetValue(formatterOptions));
                }
            }

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
            ScriptDomFormatterSettings settings = new ScriptDomFormatterSettings
            {
                AlignClauseBodies = false,
                AlignColumnDefinitionFields = true,
                AlignSetClauseItem = false,
                AllowExternalLanguagePaths = false,
                AllowExternalLibraryPaths = false,
                AsKeywordOnOwnLine = false,
                IndentSetClause = true,
                IndentationSize = 2,
                IndentViewBody = true,
                MultilineInsertSourcesList = false,
                MultilineInsertTargetsList = false,
                MultilineSelectElementsList = false,
                MultilineSetClauseItems = false,
                MultilineViewColumnsList = false,
                MultilineWherePredicatesList = false,
                NewLineBeforeCloseParenthesisInMultilineList = false,
                NewLineBeforeFromClause = false,
                NewLineBeforeGroupByClause = false,
                NewLineBeforeHavingClause = false,
                NewLineBeforeJoinClause = false,
                NewLineBeforeOffsetClause = false,
                NewLineBeforeOpenParenthesisInMultilineList = true,
                NewLineBeforeOrderByClause = false,
                NewLineBeforeOutputClause = false,
                NewLineBeforeWhereClause = false,
                NewLineBeforeWindowClause = false,
                NewlineFormattedCheckConstraint = true,
                NewLineFormattedIndexDefinition = true,
                NumNewlinesAfterStatement = 3,
                PreserveComments = false,
                SpaceBetweenDataTypeAndParameters = false,
                SpaceBetweenParametersInDataType = false
            };

            var options = ScriptDomFormatterOptionsMapper.ToScriptGeneratorOptions(settings);

            Assert.AreEqual(settings.SqlVersion, options.SqlVersion);
            Assert.AreEqual(settings.SqlEngineType, options.SqlEngineType);
            Assert.AreEqual(settings.AlignClauseBodies, options.AlignClauseBodies);
            Assert.AreEqual(settings.AlignColumnDefinitionFields, options.AlignColumnDefinitionFields);
            Assert.AreEqual(settings.AlignSetClauseItem, options.AlignSetClauseItem);
            Assert.AreEqual(settings.AllowExternalLanguagePaths, options.AllowExternalLanguagePaths);
            Assert.AreEqual(settings.AllowExternalLibraryPaths, options.AllowExternalLibraryPaths);
            Assert.AreEqual(settings.AsKeywordOnOwnLine, options.AsKeywordOnOwnLine);
            Assert.AreEqual(settings.IndentSetClause, options.IndentSetClause);
            Assert.AreEqual(settings.KeywordCasing, options.KeywordCasing);
            Assert.AreEqual(settings.IndentationSize, options.IndentationSize);
            Assert.AreEqual(settings.IndentViewBody, options.IndentViewBody);
            Assert.AreEqual(settings.MultilineInsertSourcesList, options.MultilineInsertSourcesList);
            Assert.AreEqual(settings.MultilineInsertTargetsList, options.MultilineInsertTargetsList);
            Assert.AreEqual(settings.MultilineSelectElementsList, options.MultilineSelectElementsList);
            Assert.AreEqual(settings.MultilineSetClauseItems, options.MultilineSetClauseItems);
            Assert.AreEqual(settings.MultilineViewColumnsList, options.MultilineViewColumnsList);
            Assert.AreEqual(settings.MultilineWherePredicatesList, options.MultilineWherePredicatesList);
            Assert.AreEqual(settings.NewLineBeforeCloseParenthesisInMultilineList, options.NewLineBeforeCloseParenthesisInMultilineList);
            Assert.AreEqual(settings.NewLineBeforeFromClause, options.NewLineBeforeFromClause);
            Assert.AreEqual(settings.NewLineBeforeGroupByClause, options.NewLineBeforeGroupByClause);
            Assert.AreEqual(settings.NewLineBeforeHavingClause, options.NewLineBeforeHavingClause);
            Assert.AreEqual(settings.NewLineBeforeJoinClause, options.NewLineBeforeJoinClause);
            Assert.AreEqual(settings.NewLineBeforeOffsetClause, options.NewLineBeforeOffsetClause);
            Assert.AreEqual(settings.NewLineBeforeOpenParenthesisInMultilineList, options.NewLineBeforeOpenParenthesisInMultilineList);
            Assert.AreEqual(settings.NewLineBeforeOrderByClause, options.NewLineBeforeOrderByClause);
            Assert.AreEqual(settings.NewLineBeforeOutputClause, options.NewLineBeforeOutputClause);
            Assert.AreEqual(settings.NewLineBeforeWhereClause, options.NewLineBeforeWhereClause);
            Assert.AreEqual(settings.NewLineBeforeWindowClause, options.NewLineBeforeWindowClause);
            Assert.AreEqual(settings.NewlineFormattedCheckConstraint, options.NewlineFormattedCheckConstraint);
            Assert.AreEqual(settings.NewLineFormattedIndexDefinition, options.NewLineFormattedIndexDefinition);
            Assert.AreEqual(settings.NumNewlinesAfterStatement, options.NumNewlinesAfterStatement);
            Assert.AreEqual(settings.PreserveComments, options.PreserveComments);
            Assert.AreEqual(settings.SpaceBetweenDataTypeAndParameters, options.SpaceBetweenDataTypeAndParameters);
            Assert.AreEqual(settings.SpaceBetweenParametersInDataType, options.SpaceBetweenParametersInDataType);
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
            Assert.True(settings.PreserveComments);
        }

        [Test]
        public void ResolveShouldRetainDefaultsForInvalidValues()
        {
            SqlFormatterOptions formatterOptions = new SqlFormatterOptions
            {
                SqlVersion = (SqlFormatterVersion)(-1),
                SqlEngineType = (SqlFormatterEngineType)(-1),
                KeywordCasing = (SqlFormatterKeywordCasing)(-1),
                NumNewlinesAfterStatement = 6
            };

            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.Resolve(null, formatterOptions);

            Assert.AreEqual("Sql170", settings.SqlVersion.ToString());
            Assert.AreEqual("All", settings.SqlEngineType.ToString());
            Assert.AreEqual("Uppercase", settings.KeywordCasing.ToString());
            Assert.AreEqual(1, settings.NumNewlinesAfterStatement);
            Assert.AreEqual(4, settings.IndentationSize);
        }
    }
}
