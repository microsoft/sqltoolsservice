//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that reqires knowledge of
    /// the language to perfom, such as definitions, intellisense, etc.
    /// </summary>
    public static class AutoCompleteHelper
    {
        private static CompletionItem[] emptyCompletionList = new CompletionItem[0];

        private static readonly string[] DefaultCompletionText = new string[]
        {
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
            "are",
            "as",
            "asc",
            "ascii",
            "asin",
            "at",
            "atan",
            "atn2",
            "authorization",
            "avg",
            "backup",
            "begin",
            "between",
            "binary",
            "bit",
            "both",
            "break",
            "bulk",
            "by",
            "call",
            "cascade",
            "case",
            "cast",
            "catalog",
            "catch",
            "ceiling",
            "char",
            "character",
            "charindex",
            "check",
            "checkpoint",
            "checksum_agg",
            "close",
            "clustered",
            "coalesce",
            "collate",
            "collation",
            "column",
            "columnstore",
            "commit",
            "compute",
            "concat",
            "concat_ws",
            "connect",
            "constraint",
            "constraints",
            "contains",
            "containstable",
            "continue",
            "convert",
            "cos",
            "cot",
            "count",
            "count_big",
            "create",
            "cross",
            "current",
            "current_date",
            "current_timestamp",
            "current_user",
            "cursor",
            "cursor_close_on_commit",
            "cursor_default",
            "data",
            "data_compression",
            "database",
            "datalength",
            "date",
            "dateadd",
            "datediff",
            "datefromparts",
            "datename",
            "datepart",
            "datetime",
            "datetime2",
            "day",
            "days",
            "dbcc",
            "deallocate",
            "dec",
            "decimal",
            "declare",
            "default",
            "degrees",
            "delete",
            "deny",
            "desc",
            "description",
            "difference",
            "disabled",
            "disk",
            "distinct",
            "distributed",
            "double",
            "drop",
            "drop_existing",
            "dump",
            "dynamic",
            "else",
            "enable",
            "encrypted",
            "end",
            "end-exec",
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
            "first",
            "float",
            "floor",
            "for",
            "foreign",
            "format",
            "freetexttable",
            "from",
            "full",
            "function",
            "geography",
            "get",
            "getdate",
            "getutcdate",
            "global",
            "go",
            "goto",
            "grant",
            "group",
            "grouping",
            "grouping_id",
            "hash",
            "hashed",
            "having",
            "hidden",
            "hierarchyid",
            "holdlock",
            "hour",
            "hours",
            "identity",
            "identity_insert",
            "identitycol",
            "if",
            "iif",
            "image",
            "immediate",
            "in",
            "include",
            "index",
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
            "json",
            "key",
            "language",
            "last",
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
            "max",
            "maxdop",
            "merge",
            "message",
            "min",
            "minute",
            "modify",
            "module",
            "month",
            "move",
            "names",
            "namespace",
            "national",
            "native_compilation",
            "nchar",
            "next",
            "no",
            "nocheck",
            "nocount",
            "nonclustered",
            "none",
            "norecompute",
            "not",
            "now",
            "null",
            "nullif",
            "numeric",
            "nvarchar",
            "object",
            "of",
            "off",
            "offsets",
            "on",
            "online",
            "only",
            "open",
            "openquery",
            "openrowset",
            "openxml",
            "option",
            "or",
            "order",
            "out",
            "outer",
            "output",
            "over",
            "owner",
            "partial",
            "partition",
            "password",
            "path",
            "patindex",
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
            "primary",
            "print",
            "prior",
            "proc",
            "procedure",
            "public",
            "query_store",
            "quoted_identifier",
            "quotename",
            "radians",
            "raiserror",
            "rand",
            "range",
            "raw",
            "read",
            "read_committed_snapshot",
            "read_only",
            "read_write",
            "readonly",
            "readtext",
            "real",
            "rebuild",
            "receive",
            "reconfigure",
            "recovery",
            "recursive",
            "recursive_triggers",
            "references",
            "relative",
            "remove",
            "reorganize",
            "replace",
            "replicate",
            "replication",
            "required",
            "restart",
            "restore",
            "restrict",
            "resume",
            "return",
            "returns",
            "reverse",
            "revert",
            "revoke",
            "right",
            "rollback",
            "rollup",
            "round",
            "row",
            "rowcount",
            "rowguidcol",
            "rows",
            "rtrim",
            "rule",
            "sample",
            "save",
            "schema",
            "schemabinding",
            "scoped",
            "scroll",
            "second",
            "secondary",
            "security",
            "select",
            "send",
            "sent",
            "sequence",
            "server",
            "session",
            "session_user",
            "sessionproperty",
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
            "sql",
            "sqrt",
            "square",
            "standard",
            "start",
            "started",
            "state",
            "statement",
            "static",
            "statistics",
            "statistics_norecompute",
            "status",
            "stdev",
            "stdevp",
            "stopped",
            "str",
            "string_agg",
            "stuff",
            "substring",
            "sum",
            "sysdatetime",
            "sysname",
            "system",
            "system_time",
            "system_user",
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
            "tran",
            "transaction",
            "translate",
            "trigger",
            "trim",
            "true",
            "truncate",
            "try",
            "try_convert",
            "tsql",
            "type",
            "uncommitted",
            "unicode",
            "union",
            "unique",
            "uniqueidentifier",
            "unknown",
            "unpivot",
            "update",
            "updatetext",
            "upper",
            "use",
            "user",
            "user_name",
            "using",
            "value",
            "values",
            "var",
            "varchar",
            "varp",
            "varying",
            "version",
            "view",
            "waitfor",
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
            int pos = Array.IndexOf(DefaultCompletionText, text.ToLower());
            return pos > -1;
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
                token.Text.All(ch => char.IsLetter(ch)) &&
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
                var location = locations.ParamStartLocation.Value;
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
                var location = locations.ParamEndLocation.Value;
                if (line > location.LineNumber || (line == location.LineNumber && line == location.LineNumber && column > location.ColumnNumber))
                {
                    currentParameter = -1;
                }
            }
            help.ActiveParameter = currentParameter;

            return help;
        }
    }
}
