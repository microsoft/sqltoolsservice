//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
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
        private const int PrepopulateBindTimeout = 60000;

        private static WorkspaceService<SqlToolsSettings> workspaceServiceInstance;

        private static Regex ValidSqlNameRegex = new Regex(@"^[\p{L}_@][\p{L}\p{N}@$#_]{0,127}$");

        private static CompletionItem[] emptyCompletionList = new CompletionItem[0];

        private static readonly string[] DefaultCompletionText = new string[]
        {            
            "all",
            "alter",
            "and",
            "apply",
            "as",
            "asc",
            "at",
            "backup",
            "begin",
            "binary",
            "bit",
            "break",
            "bulk",
            "by",
            "call",
            "cascade",
            "case",
            "catch",
            "char",
            "character",
            "check",
            "checkpoint",
            "close",
            "clustered",
            "column",
            "columnstore",
            "commit",
            "connect",
            "constraint",
            "continue",
            "create",
            "cross",
            "current_date",
            "cursor",
            "cursor_close_on_commit",
            "cursor_default",
            "data",
            "data_compression",
            "database",
            "date",
            "datetime",
            "datetime2",
            "days",
            "dbcc",
            "dec",
            "decimal",
            "declare",
            "default",
            "delete",
            "deny",
            "desc",
            "description",
            "disabled",
            "disk",
            "distinct",
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
            "exec",
            "execute",
            "exists",
            "exit",
            "external",
            "fast_forward",
            "fetch",
            "file",
            "filegroup",
            "filename",
            "filestream",
            "filter",
            "first",
            "float",
            "for",
            "foreign",
            "from",
            "full",
            "function",
            "geography",
            "get",
            "global",
            "go",
            "goto",
            "grant",
            "group",
            "hash",
            "hashed",
            "having",
            "hidden",
            "hierarchyid",
            "holdlock",
            "hours",
            "identity",
            "identitycol",
            "if",
            "image",
            "immediate",
            "include",
            "index",
            "inner",
            "insert",
            "instead",
            "int",
            "integer",
            "intersect",
            "into",
            "isolation",
            "join",
            "json",
            "key",
            "language",
            "last",
            "left",
            "level",
            "lineno",
            "load",
            "local",
            "locate",
            "location",
            "login",
            "masked",
            "maxdop",
            "merge",
            "message",
            "modify",
            "move",
            "namespace",
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
            "numeric",
            "object",
            "of",
            "off",
            "offsets",
            "on",
            "online",
            "open",
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
            "percent",
            "percentage",
            "period",
            "persisted",
            "plan",
            "policy",
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
            "raiserror",
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
            "required",
            "restart",
            "restore",
            "restrict",
            "resume",
            "return",
            "returns",
            "revert",
            "revoke",
            "rollback",
            "rollup",
            "row",
            "rowcount",
            "rowguidcol",
            "rows",
            "rule",
            "sample",
            "save",
            "schema",
            "schemabinding",
            "scoped",
            "scroll",
            "secondary",
            "security",
            "select",
            "send",
            "sent",
            "sequence",
            "server",
            "session",
            "set",
            "sets",
            "setuser",
            "simple",
            "smallint",
            "smallmoney",
            "snapshot",
            "sql",
            "standard",
            "start",
            "started",
            "state",
            "statement",
            "static",
            "statistics",
            "statistics_norecompute",
            "status",
            "stopped",
            "sysname",
            "system",
            "system_time",
            "table",
            "take",
            "target",
            "then",
            "throw",
            "time",
            "timestamp",
            "tinyint",
            "to",
            "top",
            "tran",
            "transaction",
            "trigger",
            "truncate",
            "try",
            "tsql",
            "type",
            "uncommitted",
            "union",
            "unique",
            "uniqueidentifier",
            "updatetext",
            "use",
            "user",
            "using",
            "value",
            "values",
            "varchar",
            "version",
            "view",
            "waitfor",
            "when",
            "where",
            "while",
            "with",
            "within",
            "without",
            "writetext",
            "xact_abort",
            "xml",
        };

        /// <summary>
        /// Gets a static instance of an empty completion list to avoid
        // unneeded memory allocations
        /// </summary>
        internal static CompletionItem[] EmptyCompletionList
        {
            get
            {
                return AutoCompleteHelper.emptyCompletionList;
            }
        }

        /// <summary>
        /// Gets or sets the current workspace service instance
        /// Setter for internal testing purposes only
        /// </summary>
        internal static WorkspaceService<SqlToolsSettings> WorkspaceServiceInstance
        {
            get
            {
                if (AutoCompleteHelper.workspaceServiceInstance == null)
                {
                    AutoCompleteHelper.workspaceServiceInstance =  WorkspaceService<SqlToolsSettings>.Instance;
                }
                return AutoCompleteHelper.workspaceServiceInstance;
            }
            set
            {
                AutoCompleteHelper.workspaceServiceInstance = value;
            }
        }        

        /// <summary>
        /// Get the default completion list from hard-coded list
        /// </summary>
        /// <param name="row"></param>
        /// <param name="startColumn"></param>
        /// <param name="endColumn"></param>
        /// <param name="useLowerCase"></param>
        internal static CompletionItem[] GetDefaultCompletionItems(
            int row, 
            int startColumn, 
            int endColumn,
            bool useLowerCase,
            string tokenText = null)
        {
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
                        useLowerCase ? completionText.ToLower() : completionText.ToUpper(),
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
            return CreateCompletionItem(label, label + " keyword", label, CompletionItemKind.Keyword, row, startColumn, endColumn);
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
                list.Insert(0, CreateCompletionItem(token.Text, token.Text, token.Text, CompletionItemKind.Text, row, startColumn, endColumn));
                return list.ToArray();
            }
            return currentList;
        }

        private static CompletionItem CreateCompletionItem(
            string label, 
            string detail,
            string insertText,
            CompletionItemKind kind,
            int row,
            int startColumn,
            int endColumn)
        {
            CompletionItem item = new CompletionItem()
            {
                Label = label,
                Kind = kind,
                Detail = detail,
                InsertText = insertText,
                TextEdit = new TextEdit
                {
                    NewText = insertText,
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
                }
            };

            return item;
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
            int endColumn)
        {           
            List<CompletionItem> completions = new List<CompletionItem>();
    
            foreach (var autoCompleteItem in suggestions)
            {
                string  insertText = GetCompletionItemInsertName(autoCompleteItem);
                CompletionItemKind kind = CompletionItemKind.Variable;
                switch (autoCompleteItem.Type)
                {
                    case DeclarationType.Schema:
                        kind = CompletionItemKind.Module;
                        break;
                    case DeclarationType.Column:
                        kind = CompletionItemKind.Field;
                        break;
                    case DeclarationType.Table:
                    case DeclarationType.View:
                        kind = CompletionItemKind.File;
                        break;
                    case DeclarationType.Database:
                        kind = CompletionItemKind.Method;
                        break;
                    case DeclarationType.ScalarValuedFunction:
                    case DeclarationType.TableValuedFunction:
                    case DeclarationType.BuiltInFunction:
                        kind = CompletionItemKind.Value;
                        break;
                    default:
                        kind = CompletionItemKind.Unit;
                        break;
                }

                // convert the completion item candidates into CompletionItems
                completions.Add(CreateCompletionItem(autoCompleteItem.Title, autoCompleteItem.Title, insertText, kind, row, startColumn, endColumn));
            }

            return completions.ToArray();
        }

        private static string GetCompletionItemInsertName(Declaration autoCompleteItem)
        {
            string insertText = autoCompleteItem.Title;
            if (!string.IsNullOrEmpty(autoCompleteItem.Title) && !ValidSqlNameRegex.IsMatch(autoCompleteItem.Title))
            {
                insertText = string.Format(CultureInfo.InvariantCulture, "[{0}]", autoCompleteItem.Title);
            }
            return insertText;
        }

        /// <summary>
        /// Preinitialize the parser and binder with common metadata.
        /// This should front load the long binding wait to the time the
        /// connection is established.  Once this is completed other binding
        /// requests should be faster.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="scriptInfo"></param>
        internal static void PrepopulateCommonMetadata(
            ConnectionInfo info, 
            ScriptParseInfo scriptInfo, 
            ConnectedBindingQueue bindingQueue)
        {
            if (scriptInfo.IsConnected)
            {
                var scriptFile = AutoCompleteHelper.WorkspaceServiceInstance.Workspace.GetFile(info.OwnerUri);                                
                LanguageService.Instance.ParseAndBind(scriptFile, info);

                if (Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {
                        QueueItem queueItem = bindingQueue.QueueBindingOperation(
                            key: scriptInfo.ConnectionKey,
                            bindingTimeout: AutoCompleteHelper.PrepopulateBindTimeout,
                            waitForLockTimeout: AutoCompleteHelper.PrepopulateBindTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {
                                // parse a simple statement that returns common metadata
                                ParseResult parseResult = Parser.Parse(
                                    "select ", 
                                    bindingContext.ParseOptions);

                                List<ParseResult> parseResults = new List<ParseResult>();
                                parseResults.Add(parseResult);
                                bindingContext.Binder.Bind(
                                    parseResults, 
                                    info.ConnectionDetails.DatabaseName, 
                                    BindMode.Batch);

                                // get the completion list from SQL Parser
                                var suggestions = Resolver.FindCompletions(
                                    parseResult, 1, 8, 
                                    bindingContext.MetadataDisplayInfoProvider); 

                                // this forces lazy evaluation of the suggestion metadata
                                AutoCompleteHelper.ConvertDeclarationsToCompletionItems(suggestions, 1, 8, 8);

                                parseResult = Parser.Parse(
                                    "exec ", 
                                    bindingContext.ParseOptions);

                                parseResults = new List<ParseResult>();
                                parseResults.Add(parseResult);
                                bindingContext.Binder.Bind(
                                    parseResults, 
                                    info.ConnectionDetails.DatabaseName, 
                                    BindMode.Batch);

                                // get the completion list from SQL Parser
                                suggestions = Resolver.FindCompletions(
                                    parseResult, 1, 6, 
                                    bindingContext.MetadataDisplayInfoProvider); 

                                // this forces lazy evaluation of the suggestion metadata
                                AutoCompleteHelper.ConvertDeclarationsToCompletionItems(suggestions, 1, 6, 6); 
                                return null;
                            });   
                
                        queueItem.ItemProcessed.WaitOne();                     
                    }
                    catch
                    {
                    }
                    finally
                    {
                        Monitor.Exit(scriptInfo.BuildingMetadataLock);
                    }
                }
            }
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
    }
}
