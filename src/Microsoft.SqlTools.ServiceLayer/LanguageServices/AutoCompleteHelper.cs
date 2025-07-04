//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Range = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Range;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that requires knowledge of
    /// the language to perform, such as definitions, intellisense, etc.
    /// </summary>
    public static class AutoCompleteHelper
    {
        private static CompletionItem[] emptyCompletionList = new CompletionItem[0];

        public static readonly string[] DefaultCompletionText = new string[]
        {
            "@@error",
            "@@identity",
            "@@rowcount",
            "abort",
            "abs",
            "acos",
            "action",
            "add",
            "all",
            "alter",
            "and",
            "any",
            "apply",
            "approx_count_distinct",
            "approx_percentile_cont",
            "approx_percentile_disc",
            "are",
            "array",
            "as",
            "asc",
            "ascii",
            "asin",
            "at time zone",
            "at",
            "atan",
            "atn2",
            "authorization",
            "auto_drop",
            "auto",
            "avg",
            "backup",
            "begin transaction",
            "begin",
            "between",
            "binary",
            "bit_count",
            "bit",
            "blockers",
            "both",
            "break",
            "bulk",
            "by",
            "call",
            "cascade",
            "case when",
            "case",
            "cast",
            "catalog",
            "catch",
            "ceiling",
            "char",
            "character varying",
            "character",
            "charindex",
            "check",
            "checkpoint",
            "checksum_agg",
            "choose",
            "close",
            "clustered",
            "coalesce",
            "collate",
            "collation",
            "column",
            "columnstore",
            "commit",
            "compress",
            "compute",
            "concat_ws",
            "concat",
            "connect",
            "constraint",
            "constraints",
            "contains",
            "containstable",
            "continue",
            "convert",
            "cos",
            "cot",
            "count_big",
            "count",
            "create",
            "cross join",
            "cross",
            "cte",
            "cume_dist",
            "current date",
            "current timestamp",
            "current user",
            "current_date",
            "current_timestamp",
            "current_user",
            "current",
            "cursor_close_on_commit",
            "cursor_default",
            "cursor",
            "data_compression",
            "data",
            "database",
            "datalength",
            "date_bucket",
            "date",
            "dateadd",
            "datediff",
            "datefromparts",
            "datename",
            "datepart",
            "datetime",
            "datetime2",
            "datetrunc",
            "day",
            "days",
            "db_name",
            "dbcc",
            "deallocate",
            "dec",
            "decimal",
            "declare",
            "decompress",
            "default",
            "degrees",
            "delete",
            "dense_rank",
            "deny",
            "desc",
            "description",
            "difference",
            "disabled",
            "disk",
            "distinct",
            "distributed",
            "double",
            "drop existing",
            "drop_existing",
            "drop",
            "dump",
            "dynamic",
            "else",
            "enable",
            "encrypted",
            "end exec",
            "end-exec",
            "end",
            "escape",
            "except",
            "exec",
            "execute",
            "exists",
            "exit",
            "exp",
            "external",
            "false",
            "fast_forward",
            "fetch",
            "file",
            "filegroup",
            "filename",
            "filestream",
            "fillfactor",
            "filter",
            "first value",
            "first_value",
            "first",
            "float",
            "floor",
            "for",
            "foreign key",
            "foreign",
            "format",
            "formatmessage",
            "freetexttable",
            "from",
            "full join",
            "full",
            "function",
            "generate_series",
            "geography",
            "geometry",
            "get_bit",
            "get",
            "getdate",
            "getutcdate",
            "global",
            "go",
            "goto",
            "grant",
            "group by",
            "group",
            "grouping_id",
            "grouping",
            "hash",
            "hashed",
            "having",
            "hidden",
            "hierarchyid",
            "hierarchyid",
            "holdlock",
            "host_name",
            "hour",
            "hours",
            "ident_current",
            "identity_insert",
            "identity",
            "identitycol",
            "if exists",
            "if",
            "ignore",
            "iif",
            "image",
            "immediate",
            "in",
            "include",
            "index",
            "inner join",
            "inner",
            "input",
            "insensitive",
            "insert",
            "instead",
            "int",
            "integer",
            "intersect",
            "interval",
            "into",
            "is",
            "isdate",
            "isnull",
            "isnumeric",
            "isolation",
            "join",
            "json_modify",
            "json_query",
            "json_value",
            "json",
            "key",
            "lag",
            "language",
            "last value",
            "last_value",
            "last",
            "lead",
            "leading",
            "left join",
            "left_shift",
            "left",
            "len",
            "level",
            "like",
            "lineno",
            "load",
            "local",
            "locate",
            "location",
            "log",
            "log10",
            "login",
            "lower",
            "ltrim",
            "masked",
            "match",
            "max_duration",
            "max",
            "maxdop",
            "merge",
            "message",
            "min",
            "minute",
            "minutes",
            "modify",
            "module",
            "month",
            "move",
            "names",
            "namespace",
            "national",
            "native_compilation",
            "nchar varying",
            "nchar",
            "newid",
            "newsequentialid",
            "next",
            "no",
            "nocheck",
            "nocount",
            "nonclustered",
            "none",
            "norecompute",
            "not null",
            "not",
            "now",
            "ntile",
            "null",
            "nullif",
            "nulls",
            "numeric",
            "nvarchar",
            "object_name",
            "object",
            "of",
            "off",
            "offsets",
            "on",
            "online",
            "only",
            "open",
            "openjson",
            "openquery",
            "openrowset",
            "openxml",
            "option",
            "or",
            "order by",
            "order",
            "out",
            "outer join",
            "outer",
            "output",
            "over",
            "owner",
            "partial",
            "partition by",
            "partition",
            "password",
            "path",
            "patindex",
            "pause",
            "percent_rank",
            "percent",
            "percentage",
            "period",
            "persisted",
            "pi",
            "pivot",
            "plan",
            "policy",
            "position",
            "power",
            "precision",
            "predicate",
            "primary key",
            "primary",
            "print",
            "prior",
            "proc",
            "procedure",
            "public",
            "quarter",
            "query_store",
            "quoted_identifier",
            "quotename",
            "radians",
            "raiserror",
            "rand",
            "range",
            "rank",
            "raw",
            "read only",
            "read write",
            "read_committed_snapshot",
            "read_only",
            "read_write",
            "read",
            "readonly",
            "readtext",
            "real",
            "rebuild",
            "receive",
            "reconfigure",
            "recovery",
            "recursive_triggers",
            "recursive",
            "references",
            "relative",
            "remove",
            "reorganize",
            "replace",
            "replicate",
            "replication",
            "required",
            "respect",
            "restart",
            "restore",
            "restrict",
            "resume",
            "return",
            "returns",
            "reverse",
            "revert",
            "revoke",
            "right join",
            "right_shift",
            "right",
            "rollback",
            "rollup",
            "round",
            "row_number",
            "row",
            "rowcount",
            "rowguidcol",
            "rows",
            "rtrim",
            "rule",
            "sample",
            "save",
            "scalar",
            "schema_name",
            "schema",
            "schemabinding",
            "scope_identity",
            "scoped",
            "scroll",
            "second",
            "secondary",
            "security",
            "select",
            "self",
            "send",
            "sent",
            "sequence",
            "server",
            "session user",
            "session_user",
            "session",
            "sessionproperty",
            "set_bit",
            "set",
            "sets",
            "setuser",
            "shutdown",
            "sign",
            "simple",
            "sin",
            "size",
            "smallint",
            "smallmoney",
            "snapshot",
            "some",
            "soundex",
            "space",
            "sql_variant",
            "sql",
            "sqrt",
            "square",
            "standard",
            "start",
            "started",
            "state",
            "statement",
            "static",
            "statistics_norecompute",
            "statistics",
            "status",
            "stdev",
            "stdevp",
            "stopped",
            "str",
            "string_agg",
            "string_escape",
            "string_split",
            "stuff",
            "substring",
            "sum",
            "suser_name",
            "sysdatetime",
            "sysname",
            "system user",
            "system_time",
            "system_user",
            "system",
            "table",
            "take",
            "tan",
            "target",
            "temporary",
            "then",
            "throw",
            "time",
            "timestamp",
            "tinyint",
            "to",
            "top",
            "trailing",
            "tran",
            "transaction",
            "translate",
            "trigger",
            "trim",
            "true",
            "truncate",
            "try_cast",
            "try_convert",
            "try_parse",
            "try",
            "tsql",
            "type",
            "uncommitted",
            "unicode",
            "union all",
            "union",
            "unique",
            "uniqueidentifier",
            "unknown",
            "unpivot",
            "update",
            "updatetext",
            "upper",
            "use",
            "user_id",
            "user_name",
            "user",
            "using",
            "value",
            "values",
            "var",
            "varchar",
            "varp",
            "varying",
            "version",
            "view",
            "wait_at_low_priority",
            "waitfor",
            "week",
            "when",
            "where",
            "while",
            "with",
            "within",
            "without",
            "work",
            "write",
            "writetext",
            "xact_abort",
            "xml",
            "year",
            "zone",
        };

        /// <summary>
        /// Gets a static instance of an empty completion list to avoid
        /// unneeded memory allocations
        /// </summary>
        internal static CompletionItem[] EmptyCompletionList
        {
            get
            {
                return AutoCompleteHelper.emptyCompletionList;
            }
        }

        /// <summary>
        /// Checks whether a given word is in the reserved
        /// word list or not
        /// </summary>
        internal static bool IsReservedWord(string text)
        {
            return DefaultCompletionText.Contains(text, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Get the default completion list from hard-coded list
        /// </summary>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        /// <param name="useLowerCase"></param>
        internal static CompletionItem[] GetDefaultCompletionItems(
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCase)
        {
            int row = scriptDocumentInfo.StartLine;
            int startColumn = scriptDocumentInfo.StartColumn;
            int endColumn = scriptDocumentInfo.EndColumn;
            string tokenText = scriptDocumentInfo.TokenText;
            // determine how many default completion items there will be
            int listSize = DefaultCompletionText.Length;
            if (!string.IsNullOrWhiteSpace(tokenText))
            {
                listSize = 0;
                foreach (var completionText in DefaultCompletionText)
                {
                    if (completionText.StartsWith(tokenText, StringComparison.OrdinalIgnoreCase))
                    {
                        ++listSize;
                    }
                }
            }

            // special case empty list to avoid unneed array allocations
            if (listSize == 0)
            {
                return emptyCompletionList;
            }

            // build the default completion list
            var completionItems = new CompletionItem[listSize];
            int completionItemIndex = 0;
            foreach (var completionText in DefaultCompletionText)
            {
                // add item to list if the tokenText is null (meaning return whole list)
                // or if the completion item begins with the tokenText
                if (string.IsNullOrWhiteSpace(tokenText) || completionText.StartsWith(tokenText, StringComparison.OrdinalIgnoreCase))
                {
                    completionItems[completionItemIndex] = CreateDefaultCompletionItem(
                        useLowerCase ? completionText.ToLowerInvariant() : completionText.ToUpperInvariant(),
                        row,
                        startColumn,
                        endColumn);
                    ++completionItemIndex;
                }
            }

            return completionItems;
        }

        /// <summary>
        /// Create a completion item from the default item text
        /// </summary>
        /// <param name="label"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        private static CompletionItem CreateDefaultCompletionItem(
            string label,
            int row,
            int startColumn,
            int endColumn)
        {
            return SqlCompletionItem.CreateCompletionItem(label, label + " keyword", label, CompletionItemKind.Keyword, row, startColumn, endColumn);
        }

        internal static CompletionItem[] AddTokenToItems(CompletionItem[] currentList, Token token, int row,
            int startColumn,
            int endColumn)
        {
            if (currentList != null &&
                token != null && !string.IsNullOrWhiteSpace(token.Text) &&
                token.Text.All(char.IsLetter) &&
                currentList.All(x => string.Compare(x.Label, token.Text, true) != 0
                ))
            {
                var list = currentList.ToList();
                list.Insert(0, SqlCompletionItem.CreateCompletionItem(token.Text, token.Text, token.Text, CompletionItemKind.Text, row, startColumn, endColumn));
                return list.ToArray();
            }
            return currentList;
        }

        /// <summary>
        /// Converts a list of Declaration objects to CompletionItem objects
        /// since VS Code expects CompletionItems but SQL Parser works with Declarations
        /// </summary>
        /// <param name="suggestions"></param>
        /// <param name="cursorRow"></param>
        /// <param name="cursorColumn"></param>
        /// <returns></returns>
        internal static CompletionItem[] ConvertDeclarationsToCompletionItems(
            IEnumerable<Declaration> suggestions,
            int row,
            int startColumn,
            int endColumn,
            string tokenText = null)
        {
            List<CompletionItem> completions = new List<CompletionItem>();

            foreach (var autoCompleteItem in suggestions)
            {
                SqlCompletionItem sqlCompletionItem = new SqlCompletionItem(autoCompleteItem, tokenText);

                // convert the completion item candidates into CompletionItems
                completions.Add(sqlCompletionItem.CreateCompletionItem(row, startColumn, endColumn));
            }

            return completions.ToArray();
        }

        /// <summary>
        /// Converts a SQL Parser QuickInfo object into a VS Code Hover object
        /// </summary>
        /// <param name="quickInfo"></param>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        internal static Hover ConvertQuickInfoToHover(
            Babel.CodeObjectQuickInfo quickInfo,
            int row,
            int startColumn,
            int endColumn)
        {
            // convert from the parser format to the VS Code wire format
            var markedStrings = new MarkedString[1];
            if (quickInfo != null)
            {
                markedStrings[0] = new MarkedString()
                {
                    Language = "SQL",
                    Value = quickInfo.Text
                };

                return new Hover()
                {
                    Contents = markedStrings,
                    Range = new Range
                    {
                        Start = new Position
                        {
                            Line = row,
                            Character = startColumn
                        },
                        End = new Position
                        {
                            Line = row,
                            Character = endColumn
                        }
                    }
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a SQL Parser List of MethodHelpText objects into a VS Code SignatureHelp object
        /// </summary>
        internal static SignatureHelp ConvertMethodHelpTextListToSignatureHelp(List<Babel.MethodHelpText> methods, Babel.MethodNameAndParamLocations locations, int line, int column)
        {
            Validate.IsNotNull(nameof(methods), methods);
            Validate.IsNotNull(nameof(locations), locations);
            Validate.IsGreaterThan(nameof(line), line, 0);
            Validate.IsGreaterThan(nameof(column), column, 0);

            SignatureHelp help = new SignatureHelp();

            help.Signatures = methods.Select(method =>
            {
                return new SignatureInformation()
                {
                    // Signature label format: <name> param1, param2, ..., paramn RETURNS <type>
                    Label = method.Name + " " + method.Parameters.Select(parameter => parameter.Display).Aggregate((l, r) => l + "," + r) + " " + method.Type,
                    Documentation = method.Description,
                    Parameters = method.Parameters.Select(parameter =>
                    {
                        return new ParameterInformation()
                        {
                            Label = parameter.Display,
                            Documentation = parameter.Description
                        };
                    }).ToArray()
                };
            }).Where(method => method.Label.Contains(locations.Name)).ToArray();

            if (help.Signatures.Length == 0)
            {
                return null;
            }

            // Find the matching method signature at the cursor's location
            // For now, take the first match (since we've already filtered by name above)
            help.ActiveSignature = 0;

            // Determine the current parameter at the cursor
            int currentParameter = -1; // Default case: not on any particular parameter
            if (locations.ParamStartLocation != null)
            {
                // Is the cursor past the function name?
                var location = locations.ParamStartLocation;
                if (line > location.LineNumber || (line == location.LineNumber && line == location.LineNumber && column >= location.ColumnNumber))
                {
                    currentParameter = 0;
                }
            }
            foreach (var location in locations.ParamSeperatorLocations)
            {
                // Is the cursor past a comma ',' and at least on the next parameter?
                if (line > location.LineNumber || (line == location.LineNumber && column > location.ColumnNumber))
                {
                    currentParameter++;
                }
            }
            if (locations.ParamEndLocation != null)
            {
                // Is the cursor past the end of the parameter list on a different token?
                var location = locations.ParamEndLocation;
                if (line > location.LineNumber || (line == location.LineNumber && line == location.LineNumber && column > location.ColumnNumber))
                {
                    currentParameter = -1;
                }
            }
            help.ActiveParameter = currentParameter;

            return help;
        }

        /// <summary>
        /// Give suggestions for sql star expansion. 
        /// </summary>
        /// <param name="scriptDocumentInfo">Document info containing the current cursor position</param>
        /// <returns>Completion item array containing the expanded star suggestion</returns>
        public static CompletionItem[] ExpandSqlStarExpression(ScriptDocumentInfo scriptDocumentInfo)
        {
            //Fetching the star expression node in sql script.
            SqlSelectStarExpression selectStarExpression = AutoCompleteHelper.TryGetSelectStarStatement(scriptDocumentInfo.ScriptParseInfo.ParseResult.Script, scriptDocumentInfo);
            if (selectStarExpression == null)
            {
                return null;
            }

            // Getting SQL object identifier for star expressions like a.* 
            SqlObjectIdentifier starObjectIdentifier = null;
            if (selectStarExpression.Children.Any())
            {
                starObjectIdentifier = (SqlObjectIdentifier)selectStarExpression.Children.ElementAt(0);
            }

            /*
            Returning no suggestions when the bound tables are null. 
            This happens when there are no existing connections for the script. 
            */
            if (selectStarExpression.BoundTables == null)
            {
                return null;
            }

            List<ITabular> boundedTableList = selectStarExpression.BoundTables.ToList();

            IList<string> columnNames = new List<string>();

            /*
             We include table names in 2 conditions.
             1. When there are multiple tables to avoid column ambiguity
             2. When there is single table with an alias
            */
            bool includeTableName = boundedTableList.Count > 1 || (boundedTableList.Count == 1 && boundedTableList[0] != boundedTableList[0].Unaliased);

            // Handing case for object identifiers where the column names will contain the identifier for eg: a.* becomes a.column_name
            if (starObjectIdentifier != null)
            {
                string objectIdentifierName = starObjectIdentifier.ObjectName.ToString();
                ITabular relatedTable = boundedTableList.Single(t => t.Name == objectIdentifierName);
                columnNames = relatedTable.Columns.Select(c => String.Format("{0}.{1}", Utils.MakeSqlBracket(objectIdentifierName), Utils.MakeSqlBracket(c.Name))).ToList();
            }
            else
            {
                foreach (var table in boundedTableList)
                {
                    foreach (var column in table.Columns)
                    {
                        if (includeTableName)
                        {
                            columnNames.Add($"{Utils.MakeSqlBracket(table.Name)}.{Utils.MakeSqlBracket(column.Name)}"); // Including table names in case of multiple tables to avoid column ambiguity errors. 
                        }
                        else
                        {
                            columnNames.Add(Utils.MakeSqlBracket(column.Name));
                        }
                    }
                }
            }

            if (columnNames == null || columnNames.Count == 0)
            {
                return null;
            }

            var insertText = String.Join(String.Format(",{0}", Environment.NewLine), columnNames.ToArray()); // Adding a new line after every column name
            var completionItems = new CompletionItem[] {
                new CompletionItem
                {
                    InsertText = insertText,
                    Label = insertText,
                    Detail = insertText,
                    Kind = CompletionItemKind.Text,
                    /*
                    Vscode/ADS only shows completion items that match the text present in the editor. However, in case of star expansion that is never going to happen as columns names are different than '*'. 
                    Therefore adding an explicit filterText that contains the original star expression to trick vscode/ADS into showing this suggestion item. 
                    */
                    FilterText = selectStarExpression.Sql,
                    Preselect = true,
                    TextEdit = new TextEdit {
                        NewText = insertText,
                        Range = new Range {
                            Start = new Position{
                                Line = scriptDocumentInfo.StartLine,
                                Character = selectStarExpression.StartLocation.ColumnNumber - 1
                            },
                            End = new Position {
                                Line = scriptDocumentInfo.StartLine,
                                Character = selectStarExpression.EndLocation.ColumnNumber - 1
                            }
                        }
                    }
                }
            };
            return completionItems;
        }

        public static SqlSelectStarExpression TryGetSelectStarStatement(SqlCodeObject currentNode, ScriptDocumentInfo scriptDocumentInfo)
        {
            if (currentNode == null || scriptDocumentInfo == null)
            {
                return null;
            }

            // Checking if the current node is a sql select star expression.
            if (currentNode is SqlSelectStarExpression)
            {
                return currentNode as SqlSelectStarExpression;
            }

            // Visiting children to get the the sql select star expression.
            foreach (SqlCodeObject child in currentNode.Children)
            {
                // Visiting only those children where the cursor is present. 
                int childStartLineNumber = child.StartLocation.LineNumber - 1;
                int childEndLineNumber = child.EndLocation.LineNumber - 1;
                SqlSelectStarExpression childStarExpression = TryGetSelectStarStatement(child, scriptDocumentInfo);
                if ((childStartLineNumber < scriptDocumentInfo.StartLine ||
                    childStartLineNumber == scriptDocumentInfo.StartLine && child.StartLocation.ColumnNumber <= scriptDocumentInfo.StartColumn) &&
                    (childEndLineNumber > scriptDocumentInfo.StartLine ||
                    childEndLineNumber == scriptDocumentInfo.StartLine && child.EndLocation.ColumnNumber >= scriptDocumentInfo.EndColumn) &&
                    childStarExpression != null)
                {
                    return childStarExpression;
                }
            }
            return null;
        }
    }
}
