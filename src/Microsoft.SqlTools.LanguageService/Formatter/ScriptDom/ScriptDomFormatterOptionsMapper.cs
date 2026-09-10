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
                BuiltInFunctionCasing = settings.BuiltInFunctionCasing,
                ClauseBodyAlignment = settings.ClauseBodyAlignment,
                ColumnAliasStyle = settings.ColumnAliasStyle,
                CommaPlacement = settings.CommaPlacement,
                LeadingCommaSpaceCount = settings.LeadingCommaSpaceCount,
                IdentifierBracketing = settings.IdentifierBracketing,
                IdentifierCasing = settings.IdentifierCasing,
                IndentSetClause = settings.IndentSetClause,
                KeywordCasing = settings.KeywordCasing,
                IndentationMode = settings.IndentationMode,
                IndentationSize = settings.IndentationSize,
                IndentViewBody = settings.IndentViewBody,
                MultilineGroupByElementsList = settings.MultilineGroupByElementsList,
                MultilineHavingPredicatesList = settings.MultilineHavingPredicatesList,
                MultilineInsertSourcesList = settings.MultilineInsertSourcesList,
                MultilineInsertTargetsList = settings.MultilineInsertTargetsList,
                MultilineInValuesList = settings.MultilineInValuesList,
                MultilineNestedFunctionCalls = settings.MultilineNestedFunctionCalls,
                MultilineOrderByElementsList = settings.MultilineOrderByElementsList,
                MultilinePartitionByElementsList = settings.MultilinePartitionByElementsList,
                MultilineProcedureParametersList = settings.MultilineProcedureParametersList,
                MultilineSelectElementsList = settings.MultilineSelectElementsList,
                MultilineSetClauseItems = settings.MultilineSetClauseItems,
                MultilineViewColumnsList = settings.MultilineViewColumnsList,
                MultilineWherePredicatesList = settings.MultilineWherePredicatesList,
                MultilineWithOptionsList = settings.MultilineWithOptionsList,
                NewLineAfterJoinKeyword = settings.NewLineAfterJoinKeyword,
                NewLineBeforeCloseParenthesisInMultilineList = settings.NewLineBeforeCloseParenthesisInMultilineList,
                NewLineBeforeFromClause = settings.NewLineBeforeFromClause,
                NewLineBeforeGroupByClause = settings.NewLineBeforeGroupByClause,
                NewLineBeforeHavingClause = settings.NewLineBeforeHavingClause,
                NewLineBeforeJoinClause = settings.NewLineBeforeJoinClause,
                NewLineBeforeOffsetClause = settings.NewLineBeforeOffsetClause,
                NewLineBeforeOnClause = settings.NewLineBeforeOnClause,
                NewLineBeforeOpenParenthesisInMultilineList = settings.NewLineBeforeOpenParenthesisInMultilineList,
                NewLineBeforeOrderByClause = settings.NewLineBeforeOrderByClause,
                NewLineBeforeOutputClause = settings.NewLineBeforeOutputClause,
                NewLineBeforeWhereClause = settings.NewLineBeforeWhereClause,
                NewLineBeforeWindowClause = settings.NewLineBeforeWindowClause,
                NewlineFormattedCheckConstraint = settings.NewlineFormattedCheckConstraint,
                NewLineFormattedIndexDefinition = settings.NewLineFormattedIndexDefinition,
                NumNewlinesAfterBatches = settings.NumNewlinesAfterBatches,
                NumNewlinesAfterBatchStatement = settings.NumNewlinesAfterBatchStatement,
                NumNewlinesAfterStatement = settings.NumNewlinesAfterStatement,
                PersistTrailingGo = settings.PersistTrailingGo,
                PreserveComments = settings.PreserveComments,
                SpaceBetweenDataTypeAndParameters = settings.SpaceBetweenDataTypeAndParameters,
                SpaceBetweenParametersInDataType = settings.SpaceBetweenParametersInDataType,
                TerminateBlockStatements = settings.TerminateBlockStatements
            };

            return options;
        }
    }
}
