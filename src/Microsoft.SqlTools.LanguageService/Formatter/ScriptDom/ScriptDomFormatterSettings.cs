//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.TransactSql.ScriptDom;

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

        public bool IncludeSemicolons { get; set; }

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

        public static ScriptDomFormatterSettings FromFormatOptions(FormatOptions formatOptions)
        {
            ScriptDomFormatterSettings settings = new ScriptDomFormatterSettings();
            if (formatOptions == null)
            {
                return settings;
            }

            settings.AlignColumnDefinitionFields = formatOptions.AlignColumnDefinitionsInColumns;
            settings.IndentationSize = formatOptions.SpacesPerIndent > 0
                ? formatOptions.SpacesPerIndent
                : settings.IndentationSize;
            settings.KeywordCasing = ToScriptDomKeywordCasing(formatOptions.KeywordCasing);

            settings.NewLineBeforeFromClause = formatOptions.PlaceEachReferenceOnNewLineInQueryStatements;
            settings.NewLineBeforeOrderByClause = formatOptions.PlaceEachReferenceOnNewLineInQueryStatements;
            settings.NewLineBeforeWhereClause = formatOptions.PlaceEachReferenceOnNewLineInQueryStatements;

            return settings;
        }

        private static KeywordCasing ToScriptDomKeywordCasing(CasingOptions casing)
        {
            switch (casing)
            {
                case CasingOptions.Lowercase:
                    return KeywordCasing.Lowercase;
                case CasingOptions.Uppercase:
                case CasingOptions.None:
                default:
                    return KeywordCasing.Uppercase;
            }
        }
    }
}
