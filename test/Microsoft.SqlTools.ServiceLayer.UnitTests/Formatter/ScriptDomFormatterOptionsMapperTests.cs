//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.LanguageService.Formatter;
using Microsoft.SqlTools.LanguageService.Formatter.ScriptDom;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class ScriptDomFormatterOptionsMapperTests
    {
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
                IncludeSemicolons = true,
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
            Assert.AreEqual(settings.IncludeSemicolons, options.IncludeSemicolons);
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
        public void FromFormatOptionsShouldOverlayExistingFormatterOptions()
        {
            FormatOptions formatOptions = new FormatOptions
            {
                AlignColumnDefinitionsInColumns = true,
                KeywordCasing = CasingOptions.Lowercase,
                PlaceEachReferenceOnNewLineInQueryStatements = true,
                SpacesPerIndent = 2
            };

            ScriptDomFormatterSettings settings = ScriptDomFormatterSettings.FromFormatOptions(formatOptions);

            Assert.True(settings.AlignColumnDefinitionFields);
            Assert.AreEqual("Lowercase", settings.KeywordCasing.ToString());
            Assert.True(settings.NewLineBeforeFromClause);
            Assert.True(settings.NewLineBeforeOrderByClause);
            Assert.True(settings.NewLineBeforeWhereClause);
            Assert.AreEqual(2, settings.IndentationSize);
            Assert.False(settings.IncludeSemicolons);
            Assert.True(settings.PreserveComments);
        }
    }
}
