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

        public KeywordCasing KeywordCasing { get; set; } = KeywordCasing.Uppercase;

        public bool PreserveComments { get; set; } = true;

        public bool IndentSetClause { get; set; }

        public int IndentationSize { get; set; } = 4;

        public bool IndentViewBody { get; set; }

        public bool MultilineInsertSourcesList { get; set; } = true;

        public bool MultilineInsertTargetsList { get; set; } = true;

        public bool MultilineSelectElementsList { get; set; } = true;

        public bool MultilineSetClauseItems { get; set; } = true;

        public bool MultilineViewColumnsList { get; set; } = true;

        public bool MultilineWherePredicatesList { get; set; } = true;

        public bool NewLineBeforeCloseParenthesisInMultilineList { get; set; } = true;

        public bool NewLineBeforeFromClause { get; set; } = true;

        public bool NewLineBeforeGroupByClause { get; set; } = true;

        public bool NewLineBeforeHavingClause { get; set; } = true;

        public bool NewLineBeforeJoinClause { get; set; } = true;

        public bool NewLineBeforeOffsetClause { get; set; } = true;

        public bool NewLineBeforeOpenParenthesisInMultilineList { get; set; }

        public bool NewLineBeforeOrderByClause { get; set; } = true;

        public bool NewLineBeforeOutputClause { get; set; } = true;

        public bool NewLineBeforeWhereClause { get; set; } = true;

        public bool NewLineBeforeWindowClause { get; set; } = true;

        public bool NewlineFormattedCheckConstraint { get; set; }

        public bool NewLineFormattedIndexDefinition { get; set; }

        public int NumNewlinesAfterStatement { get; set; } = 1;

        public bool SpaceBetweenDataTypeAndParameters { get; set; } = true;

        public bool SpaceBetweenParametersInDataType { get; set; } = true;

        public static ScriptDomFormatterSettings Resolve(
            FormattingOptions formattingOptions,
            SqlFormatterOptions formatterOptions)
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
                KeywordCasing? keywordCasing = ToScriptDomKeywordCasing(formatterOptions.KeywordCasing);
                if (keywordCasing.HasValue)
                {
                    settings.KeywordCasing = keywordCasing.Value;
                }
                settings.PreserveComments = formatterOptions.PreserveComments;
                settings.IndentSetClause = formatterOptions.IndentSetClause;
                settings.IndentViewBody = formatterOptions.IndentViewBody;
                settings.MultilineInsertSourcesList = formatterOptions.MultilineInsertSourcesList;
                settings.MultilineInsertTargetsList = formatterOptions.MultilineInsertTargetsList;
                settings.MultilineSelectElementsList = formatterOptions.MultilineSelectElementsList;
                settings.MultilineSetClauseItems = formatterOptions.MultilineSetClauseItems;
                settings.MultilineViewColumnsList = formatterOptions.MultilineViewColumnsList;
                settings.MultilineWherePredicatesList = formatterOptions.MultilineWherePredicatesList;
                settings.NewLineBeforeCloseParenthesisInMultilineList = formatterOptions.NewLineBeforeCloseParenthesisInMultilineList;
                settings.NewLineBeforeFromClause = formatterOptions.NewLineBeforeFromClause;
                settings.NewLineBeforeGroupByClause = formatterOptions.NewLineBeforeGroupByClause;
                settings.NewLineBeforeHavingClause = formatterOptions.NewLineBeforeHavingClause;
                settings.NewLineBeforeJoinClause = formatterOptions.NewLineBeforeJoinClause;
                settings.NewLineBeforeOffsetClause = formatterOptions.NewLineBeforeOffsetClause;
                settings.NewLineBeforeOpenParenthesisInMultilineList = formatterOptions.NewLineBeforeOpenParenthesisInMultilineList;
                settings.NewLineBeforeOrderByClause = formatterOptions.NewLineBeforeOrderByClause;
                settings.NewLineBeforeOutputClause = formatterOptions.NewLineBeforeOutputClause;
                settings.NewLineBeforeWhereClause = formatterOptions.NewLineBeforeWhereClause;
                settings.NewLineBeforeWindowClause = formatterOptions.NewLineBeforeWindowClause;
                settings.NewlineFormattedCheckConstraint = formatterOptions.NewlineFormattedCheckConstraint;
                settings.NewLineFormattedIndexDefinition = formatterOptions.NewLineFormattedIndexDefinition;
                if (formatterOptions.NumNewlinesAfterStatement >= 0
                    && formatterOptions.NumNewlinesAfterStatement <= 5)
                {
                    settings.NumNewlinesAfterStatement = formatterOptions.NumNewlinesAfterStatement;
                }
                settings.SpaceBetweenDataTypeAndParameters = formatterOptions.SpaceBetweenDataTypeAndParameters;
                settings.SpaceBetweenParametersInDataType = formatterOptions.SpaceBetweenParametersInDataType;
            }

            if (formattingOptions?.TabSize > 0)
            {
                settings.IndentationSize = formattingOptions.TabSize;
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
    }
}
