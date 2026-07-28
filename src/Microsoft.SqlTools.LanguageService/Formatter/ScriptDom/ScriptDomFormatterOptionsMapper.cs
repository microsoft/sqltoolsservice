//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.SqlTools.LanguageService.Formatter.ScriptDom
{
    internal static class ScriptDomFormatterOptionsMapper
    {
        internal static SqlScriptGeneratorOptions ToScriptGeneratorOptions(ScriptDomFormatterSettings settings)
        {
            SqlScriptGeneratorOptions options = new SqlScriptGeneratorOptions
            {
                SqlVersion = settings.SqlVersion,
                SqlEngineType = settings.SqlEngineType,
                AlignClauseBodies = settings.AlignClauseBodies,
                AlignColumnDefinitionFields = settings.AlignColumnDefinitionFields,
                AlignSetClauseItem = settings.AlignSetClauseItem,
                AllowExternalLanguagePaths = settings.AllowExternalLanguagePaths,
                AllowExternalLibraryPaths = settings.AllowExternalLibraryPaths,
                AsKeywordOnOwnLine = settings.AsKeywordOnOwnLine,
                IndentSetClause = settings.IndentSetClause,
                KeywordCasing = settings.KeywordCasing,
                IndentationSize = settings.IndentationSize,
                IndentViewBody = settings.IndentViewBody,
                MultilineInsertSourcesList = settings.MultilineInsertSourcesList,
                MultilineInsertTargetsList = settings.MultilineInsertTargetsList,
                MultilineSelectElementsList = settings.MultilineSelectElementsList,
                MultilineSetClauseItems = settings.MultilineSetClauseItems,
                MultilineViewColumnsList = settings.MultilineViewColumnsList,
                MultilineWherePredicatesList = settings.MultilineWherePredicatesList,
                NewLineBeforeCloseParenthesisInMultilineList = settings.NewLineBeforeCloseParenthesisInMultilineList,
                NewLineBeforeFromClause = settings.NewLineBeforeFromClause,
                NewLineBeforeGroupByClause = settings.NewLineBeforeGroupByClause,
                NewLineBeforeHavingClause = settings.NewLineBeforeHavingClause,
                NewLineBeforeJoinClause = settings.NewLineBeforeJoinClause,
                NewLineBeforeOffsetClause = settings.NewLineBeforeOffsetClause,
                NewLineBeforeOpenParenthesisInMultilineList = settings.NewLineBeforeOpenParenthesisInMultilineList,
                NewLineBeforeOrderByClause = settings.NewLineBeforeOrderByClause,
                NewLineBeforeOutputClause = settings.NewLineBeforeOutputClause,
                NewLineBeforeWhereClause = settings.NewLineBeforeWhereClause,
                NewLineBeforeWindowClause = settings.NewLineBeforeWindowClause,
                NewlineFormattedCheckConstraint = settings.NewlineFormattedCheckConstraint,
                NewLineFormattedIndexDefinition = settings.NewLineFormattedIndexDefinition,
                NumNewlinesAfterStatement = settings.NumNewlinesAfterStatement,
                PreserveComments = settings.PreserveComments,
                SpaceBetweenDataTypeAndParameters = settings.SpaceBetweenDataTypeAndParameters,
                SpaceBetweenParametersInDataType = settings.SpaceBetweenParametersInDataType
            };

            return options;
        }
    }
}
