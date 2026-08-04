//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.SqlTools.LanguageService.Formatter
{
    public enum SqlFormatterVersion
    {
        Sql80,
        Sql90,
        Sql100,
        Sql110,
        Sql120,
        Sql130,
        Sql140,
        Sql150,
        Sql160,
        Sql170
    }

    public enum SqlFormatterEngineType
    {
        All,
        Standalone,
        SqlAzure
    }

    public enum SqlFormatterKeywordCasing
    {
        Lowercase,
        Uppercase,
        PascalCase
    }

    /// <summary>
    /// SQL-specific formatting options supplied through workspace configuration.
    /// </summary>
    public class SqlFormatterOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SqlFormatterVersion SqlVersion { get; set; } = SqlFormatterVersion.Sql170;

        [JsonConverter(typeof(StringEnumConverter))]
        public SqlFormatterEngineType SqlEngineType { get; set; } = SqlFormatterEngineType.All;

        public bool AlignClauseBodies { get; set; } = true;

        public bool AlignColumnDefinitionFields { get; set; } = true;

        public bool AlignSetClauseItem { get; set; } = true;

        public bool AllowExternalLanguagePaths { get; set; } = true;

        public bool AllowExternalLibraryPaths { get; set; } = true;

        public bool AsKeywordOnOwnLine { get; set; } = true;

        [JsonConverter(typeof(StringEnumConverter))]
        public SqlFormatterKeywordCasing KeywordCasing { get; set; } = SqlFormatterKeywordCasing.Uppercase;

        public bool PreserveComments { get; set; } = true;

        public bool IndentSetClause { get; set; }

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
    }
}
