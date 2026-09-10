//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlTools.LanguageService.Formatter.Contracts;

namespace Microsoft.SqlTools.LanguageService.Formatter.ScriptDom
{
    internal sealed class ScriptDomFormatterSettings
    {
        public SqlVersion SqlVersion { get; set; } = SqlVersion.Sql170;

        public SqlEngineType SqlEngineType { get; set; } = SqlEngineType.All;

        public bool AlignClauseBodies { get; set; } = true;

        public bool AlignColumnDefinitionFields { get; set; } = true;

        public bool AlignSetClauseItem { get; set; } = true;

        public bool AllowExternalLanguagePaths { get; set; } = true;

        public bool AllowExternalLibraryPaths { get; set; } = true;

        public bool AsKeywordOnOwnLine { get; set; } = true;

        public BuiltInFunctionCasing BuiltInFunctionCasing { get; set; } = BuiltInFunctionCasing.Preserve;

        public ClauseBodyAlignment ClauseBodyAlignment { get; set; } = ClauseBodyAlignment.Aligned;

        public ColumnAliasStyle ColumnAliasStyle { get; set; } = ColumnAliasStyle.AsKeyword;

        public CommaPlacement CommaPlacement { get; set; } = CommaPlacement.Trailing;

        public int LeadingCommaSpaceCount { get; set; } = 1;

        public IdentifierBracketing IdentifierBracketing { get; set; } = IdentifierBracketing.Preserve;

        public IdentifierCasing IdentifierCasing { get; set; } = IdentifierCasing.Preserve;

        public KeywordCasing KeywordCasing { get; set; } = KeywordCasing.Uppercase;

        public bool PreserveComments { get; set; } = true;

        public bool IndentSetClause { get; set; }

        public int IndentationSize { get; set; } = 4;

        public IndentationMode IndentationMode { get; set; } = IndentationMode.Spaces;

        public bool IndentViewBody { get; set; }

        public bool MultilineGroupByElementsList { get; set; }

        public bool MultilineHavingPredicatesList { get; set; } = true;

        public bool MultilineInsertSourcesList { get; set; } = true;

        public bool MultilineInsertTargetsList { get; set; } = true;

        public bool MultilineInValuesList { get; set; }

        public bool MultilineNestedFunctionCalls { get; set; }

        public bool MultilineOrderByElementsList { get; set; }

        public bool MultilinePartitionByElementsList { get; set; }

        public bool MultilineProcedureParametersList { get; set; }

        public bool MultilineSelectElementsList { get; set; } = true;

        public bool MultilineSetClauseItems { get; set; } = true;

        public bool MultilineViewColumnsList { get; set; } = true;

        public bool MultilineWherePredicatesList { get; set; } = true;

        public bool MultilineWithOptionsList { get; set; }

        public bool NewLineAfterJoinKeyword { get; set; } = true;

        public bool NewLineBeforeCloseParenthesisInMultilineList { get; set; } = true;

        public bool NewLineBeforeFromClause { get; set; } = true;

        public bool NewLineBeforeGroupByClause { get; set; } = true;

        public bool NewLineBeforeHavingClause { get; set; } = true;

        public bool NewLineBeforeJoinClause { get; set; } = true;

        public bool NewLineBeforeOffsetClause { get; set; } = true;

        public bool NewLineBeforeOnClause { get; set; } = true;

        public bool NewLineBeforeOpenParenthesisInMultilineList { get; set; }

        public bool NewLineBeforeOrderByClause { get; set; } = true;

        public bool NewLineBeforeOutputClause { get; set; } = true;

        public bool NewLineBeforeWhereClause { get; set; } = true;

        public bool NewLineBeforeWindowClause { get; set; } = true;

        public bool NewlineFormattedCheckConstraint { get; set; }

        public bool NewLineFormattedIndexDefinition { get; set; }

        public int NumNewlinesAfterBatches { get; set; } = 1;

        public int NumNewlinesAfterBatchStatement { get; set; } = 2;

        public int NumNewlinesAfterStatement { get; set; } = 1;

        public bool PersistTrailingGo { get; set; }

        public bool SpaceBetweenDataTypeAndParameters { get; set; } = true;

        public bool SpaceBetweenParametersInDataType { get; set; } = true;

        public bool TerminateBlockStatements { get; set; }

        public static ScriptDomFormatterSettings Resolve(
            FormattingOptions? formattingOptions,
            SqlFormatterOptions? formatterOptions)
        {
            ScriptDomFormatterSettings settings = new ScriptDomFormatterSettings();
            if (formatterOptions != null)
            {
                SqlVersion? sqlVersion = ToScriptDomSqlVersion(formatterOptions.SqlVersion);
                if (sqlVersion.HasValue)
                {
                    settings.SqlVersion = sqlVersion.Value;
                }

                SqlEngineType? sqlEngineType = ToScriptDomSqlEngineType(formatterOptions.SqlEngineType);
                if (sqlEngineType.HasValue)
                {
                    settings.SqlEngineType = sqlEngineType.Value;
                }

                settings.AlignClauseBodies = formatterOptions.AlignClauseBodies;
                settings.AlignColumnDefinitionFields = formatterOptions.AlignColumnDefinitionFields;
                settings.AlignSetClauseItem = formatterOptions.AlignSetClauseItem;
                settings.AllowExternalLanguagePaths = formatterOptions.AllowExternalLanguagePaths;
                settings.AllowExternalLibraryPaths = formatterOptions.AllowExternalLibraryPaths;
                settings.AsKeywordOnOwnLine = formatterOptions.AsKeywordOnOwnLine;
                BuiltInFunctionCasing? builtInFunctionCasing = ToScriptDomBuiltInFunctionCasing(formatterOptions.BuiltInFunctionCasing);
                if (builtInFunctionCasing.HasValue)
                {
                    settings.BuiltInFunctionCasing = builtInFunctionCasing.Value;
                }

                ClauseBodyAlignment? clauseBodyAlignment = ToScriptDomClauseBodyAlignment(formatterOptions.ClauseBodyAlignment);
                if (clauseBodyAlignment.HasValue)
                {
                    settings.ClauseBodyAlignment = clauseBodyAlignment.Value;
                }

                ColumnAliasStyle? columnAliasStyle = ToScriptDomColumnAliasStyle(formatterOptions.ColumnAliasStyle);
                if (columnAliasStyle.HasValue)
                {
                    settings.ColumnAliasStyle = columnAliasStyle.Value;
                }

                CommaPlacement? commaPlacement = ToScriptDomCommaPlacement(formatterOptions.CommaPlacement);
                if (commaPlacement.HasValue)
                {
                    settings.CommaPlacement = commaPlacement.Value;
                }

                if (formatterOptions.LeadingCommaSpaceCount >= 0
                    && formatterOptions.LeadingCommaSpaceCount <= 1)
                {
                    settings.LeadingCommaSpaceCount = formatterOptions.LeadingCommaSpaceCount;
                }

                IdentifierBracketing? identifierBracketing = ToScriptDomIdentifierBracketing(formatterOptions.IdentifierBracketing);
                if (identifierBracketing.HasValue)
                {
                    settings.IdentifierBracketing = identifierBracketing.Value;
                }

                IdentifierCasing? identifierCasing = ToScriptDomIdentifierCasing(formatterOptions.IdentifierCasing);
                if (identifierCasing.HasValue)
                {
                    settings.IdentifierCasing = identifierCasing.Value;
                }

                KeywordCasing? keywordCasing = ToScriptDomKeywordCasing(formatterOptions.KeywordCasing);
                if (keywordCasing.HasValue)
                {
                    settings.KeywordCasing = keywordCasing.Value;
                }

                settings.PreserveComments = formatterOptions.PreserveComments;
                settings.IndentSetClause = formatterOptions.IndentSetClause;
                settings.IndentViewBody = formatterOptions.IndentViewBody;
                settings.MultilineGroupByElementsList = formatterOptions.MultilineGroupByElementsList;
                settings.MultilineHavingPredicatesList = formatterOptions.MultilineHavingPredicatesList;
                settings.MultilineInsertSourcesList = formatterOptions.MultilineInsertSourcesList;
                settings.MultilineInsertTargetsList = formatterOptions.MultilineInsertTargetsList;
                settings.MultilineInValuesList = formatterOptions.MultilineInValuesList;
                settings.MultilineNestedFunctionCalls = formatterOptions.MultilineNestedFunctionCalls;
                settings.MultilineOrderByElementsList = formatterOptions.MultilineOrderByElementsList;
                settings.MultilinePartitionByElementsList = formatterOptions.MultilinePartitionByElementsList;
                settings.MultilineProcedureParametersList = formatterOptions.MultilineProcedureParametersList;
                settings.MultilineSelectElementsList = formatterOptions.MultilineSelectElementsList;
                settings.MultilineSetClauseItems = formatterOptions.MultilineSetClauseItems;
                settings.MultilineViewColumnsList = formatterOptions.MultilineViewColumnsList;
                settings.MultilineWherePredicatesList = formatterOptions.MultilineWherePredicatesList;
                settings.MultilineWithOptionsList = formatterOptions.MultilineWithOptionsList;
                settings.NewLineAfterJoinKeyword = formatterOptions.NewLineAfterJoinKeyword;
                settings.NewLineBeforeCloseParenthesisInMultilineList = formatterOptions.NewLineBeforeCloseParenthesisInMultilineList;
                settings.NewLineBeforeFromClause = formatterOptions.NewLineBeforeFromClause;
                settings.NewLineBeforeGroupByClause = formatterOptions.NewLineBeforeGroupByClause;
                settings.NewLineBeforeHavingClause = formatterOptions.NewLineBeforeHavingClause;
                settings.NewLineBeforeJoinClause = formatterOptions.NewLineBeforeJoinClause;
                settings.NewLineBeforeOffsetClause = formatterOptions.NewLineBeforeOffsetClause;
                settings.NewLineBeforeOnClause = formatterOptions.NewLineBeforeOnClause;
                settings.NewLineBeforeOpenParenthesisInMultilineList = formatterOptions.NewLineBeforeOpenParenthesisInMultilineList;
                settings.NewLineBeforeOrderByClause = formatterOptions.NewLineBeforeOrderByClause;
                settings.NewLineBeforeOutputClause = formatterOptions.NewLineBeforeOutputClause;
                settings.NewLineBeforeWhereClause = formatterOptions.NewLineBeforeWhereClause;
                settings.NewLineBeforeWindowClause = formatterOptions.NewLineBeforeWindowClause;
                settings.NewlineFormattedCheckConstraint = formatterOptions.NewlineFormattedCheckConstraint;
                settings.NewLineFormattedIndexDefinition = formatterOptions.NewLineFormattedIndexDefinition;
                if (IsValidNewlineCount(formatterOptions.NumNewlinesAfterBatches))
                {
                    settings.NumNewlinesAfterBatches = formatterOptions.NumNewlinesAfterBatches;
                }

                if (IsValidNewlineCount(formatterOptions.NumNewlinesAfterBatchStatement))
                {
                    settings.NumNewlinesAfterBatchStatement = formatterOptions.NumNewlinesAfterBatchStatement;
                }

                if (IsValidNewlineCount(formatterOptions.NumNewlinesAfterStatement))
                {
                    settings.NumNewlinesAfterStatement = formatterOptions.NumNewlinesAfterStatement;
                }

                settings.PersistTrailingGo = formatterOptions.PersistTrailingGo;
                settings.SpaceBetweenDataTypeAndParameters = formatterOptions.SpaceBetweenDataTypeAndParameters;
                settings.SpaceBetweenParametersInDataType = formatterOptions.SpaceBetweenParametersInDataType;
                settings.TerminateBlockStatements = formatterOptions.TerminateBlockStatements;
            }

            if (formattingOptions != null)
            {
                if (formattingOptions.TabSize > 0)
                {
                    settings.IndentationSize = formattingOptions.TabSize;
                }
                settings.IndentationMode = formattingOptions.InsertSpaces
                    ? IndentationMode.Spaces
                    : IndentationMode.Tabs;
            }

            return settings;
        }

        private static SqlVersion? ToScriptDomSqlVersion(SqlFormatterVersion sqlVersion)
        {
            switch (sqlVersion)
            {
                case SqlFormatterVersion.Sql80:
                    return SqlVersion.Sql80;
                case SqlFormatterVersion.Sql90:
                    return SqlVersion.Sql90;
                case SqlFormatterVersion.Sql100:
                    return SqlVersion.Sql100;
                case SqlFormatterVersion.Sql110:
                    return SqlVersion.Sql110;
                case SqlFormatterVersion.Sql120:
                    return SqlVersion.Sql120;
                case SqlFormatterVersion.Sql130:
                    return SqlVersion.Sql130;
                case SqlFormatterVersion.Sql140:
                    return SqlVersion.Sql140;
                case SqlFormatterVersion.Sql150:
                    return SqlVersion.Sql150;
                case SqlFormatterVersion.Sql160:
                    return SqlVersion.Sql160;
                case SqlFormatterVersion.Sql170:
                    return SqlVersion.Sql170;
                case SqlFormatterVersion.Sql180:
                    return SqlVersion.Sql180;
                case SqlFormatterVersion.SqlFabricDW:
                    return SqlVersion.SqlFabricDW;
                default:
                    return null;
            }
        }

        private static BuiltInFunctionCasing? ToScriptDomBuiltInFunctionCasing(SqlFormatterBuiltInFunctionCasing casing)
        {
            switch (casing)
            {
                case SqlFormatterBuiltInFunctionCasing.Preserve:
                    return BuiltInFunctionCasing.Preserve;
                case SqlFormatterBuiltInFunctionCasing.Uppercase:
                    return BuiltInFunctionCasing.Uppercase;
                case SqlFormatterBuiltInFunctionCasing.Lowercase:
                    return BuiltInFunctionCasing.Lowercase;
                case SqlFormatterBuiltInFunctionCasing.PascalCase:
                    return BuiltInFunctionCasing.PascalCase;
                default:
                    return null;
            }
        }

        private static ClauseBodyAlignment? ToScriptDomClauseBodyAlignment(SqlFormatterClauseBodyAlignment alignment)
        {
            switch (alignment)
            {
                case SqlFormatterClauseBodyAlignment.Aligned:
                    return ClauseBodyAlignment.Aligned;
                case SqlFormatterClauseBodyAlignment.Indented:
                    return ClauseBodyAlignment.Indented;
                default:
                    return null;
            }
        }

        private static ColumnAliasStyle? ToScriptDomColumnAliasStyle(SqlFormatterColumnAliasStyle style)
        {
            switch (style)
            {
                case SqlFormatterColumnAliasStyle.AsKeyword:
                    return ColumnAliasStyle.AsKeyword;
                case SqlFormatterColumnAliasStyle.EqualsSign:
                    return ColumnAliasStyle.EqualsSign;
                case SqlFormatterColumnAliasStyle.Preserve:
                    return ColumnAliasStyle.Preserve;
                default:
                    return null;
            }
        }

        private static CommaPlacement? ToScriptDomCommaPlacement(SqlFormatterCommaPlacement placement)
        {
            switch (placement)
            {
                case SqlFormatterCommaPlacement.Trailing:
                    return CommaPlacement.Trailing;
                case SqlFormatterCommaPlacement.Leading:
                    return CommaPlacement.Leading;
                default:
                    return null;
            }
        }

        private static IdentifierBracketing? ToScriptDomIdentifierBracketing(SqlFormatterIdentifierBracketing bracketing)
        {
            switch (bracketing)
            {
                case SqlFormatterIdentifierBracketing.Preserve:
                    return IdentifierBracketing.Preserve;
                case SqlFormatterIdentifierBracketing.IncludeBrackets:
                    return IdentifierBracketing.IncludeBrackets;
                case SqlFormatterIdentifierBracketing.ExcludeBrackets:
                    return IdentifierBracketing.ExcludeBrackets;
                default:
                    return null;
            }
        }

        private static IdentifierCasing? ToScriptDomIdentifierCasing(SqlFormatterIdentifierCasing casing)
        {
            switch (casing)
            {
                case SqlFormatterIdentifierCasing.Preserve:
                    return IdentifierCasing.Preserve;
                case SqlFormatterIdentifierCasing.Uppercase:
                    return IdentifierCasing.Uppercase;
                case SqlFormatterIdentifierCasing.Lowercase:
                    return IdentifierCasing.Lowercase;
                case SqlFormatterIdentifierCasing.PascalCase:
                    return IdentifierCasing.PascalCase;
                default:
                    return null;
            }
        }

        private static SqlEngineType? ToScriptDomSqlEngineType(SqlFormatterEngineType sqlEngineType)
        {
            switch (sqlEngineType)
            {
                case SqlFormatterEngineType.All:
                    return SqlEngineType.All;
                case SqlFormatterEngineType.Standalone:
                    return SqlEngineType.Standalone;
                case SqlFormatterEngineType.SqlAzure:
                    return SqlEngineType.SqlAzure;
                default:
                    return null;
            }
        }

        private static KeywordCasing? ToScriptDomKeywordCasing(SqlFormatterKeywordCasing keywordCasing)
        {
            switch (keywordCasing)
            {
                case SqlFormatterKeywordCasing.Lowercase:
                    return KeywordCasing.Lowercase;
                case SqlFormatterKeywordCasing.Uppercase:
                    return KeywordCasing.Uppercase;
                case SqlFormatterKeywordCasing.PascalCase:
                    return KeywordCasing.PascalCase;
                default:
                    return null;
            }
        }

        private static bool IsValidNewlineCount(int value)
        {
            return value >= 0 && value <= 5;
        }
    }
}
