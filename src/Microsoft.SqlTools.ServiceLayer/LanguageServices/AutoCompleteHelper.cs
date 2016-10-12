//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
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

        private static readonly string[] DefaultCompletionText = new string[]
        {
            "absolute",
            "accent_sensitivity",
            "action",
            "activation",
            "add",
            "address",
            "admin",
            "after",
            "aggregate",
            "algorithm",
            "allow_page_locks",
            "allow_row_locks",
            "allow_snapshot_isolation",
            "alter",
            "always",
            "ansi_null_default",
            "ansi_nulls",
            "ansi_padding",
            "ansi_warnings",
            "application",
            "arithabort",
            "as",
            "asc",
            "assembly",
            "asymmetric",
            "at",
            "atomic",
            "audit",
            "authentication",
            "authorization",
            "auto",
            "auto_close",
            "auto_shrink",
            "auto_update_statistics",
            "auto_update_statistics_async",
            "availability",
            "backup",
            "before",
            "begin",
            "binary",
            "bit",
            "block",
            "break",
            "browse",
            "bucket_count",
            "bulk",
            "by",
            "call",
            "caller",
            "card",
            "cascade",
            "case",
            "catalog",
            "catch",
            "change_tracking",
            "changes",
            "char",
            "character",
            "check",
            "checkpoint",
            "close",
            "clustered",
            "collection",
            "column",
            "column_encryption_key",
            "columnstore",
            "commit",
            "compatibility_level",
            "compress_all_row_groups",
            "compression",
            "compression_delay",
            "compute",
            "concat_null_yields_null",
            "configuration",
            "connect",
            "constraint",
            "containstable",
            "continue",
            "create",
            "cube",
            "current",
            "current_date",
            "cursor",
            "cursor_close_on_commit",
            "cursor_default",
            "data",
            "data_compression",
            "database",
            "date",
            "date_correlation_optimization",
            "datefirst",
            "datetime",
            "datetime2",
            "days",
            "db_chaining",
            "dbcc",
            "deallocate",
            "dec",
            "decimal",
            "declare",
            "default",
            "delayed_durability",
            "delete",
            "deny",
            "desc",
            "description",
            "disable_broker",
            "disabled",
            "disk",
            "distinct",
            "distributed",
            "double",
            "drop",
            "drop_existing",
            "dump",
            "durability",
            "dynamic",
            "else",
            "enable",
            "encrypted",
            "encryption_type",
            "end",
            "end-exec",
            "entry",
            "errlvl",
            "escape",
            "event",
            "except",
            "exec",
            "execute",
            "exit",
            "external",
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
            "for",
            "foreign",
            "freetext",
            "freetexttable",
            "from",
            "full",
            "fullscan",
            "fulltext",
            "function",
            "generated",
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
            "identity_insert",
            "identitycol",
            "if",
            "ignore_dup_key",
            "image",
            "immediate",
            "include",
            "index",
            "inflectional",
            "insensitive",
            "insert",
            "instead",
            "int",
            "integer",
            "integrated",
            "intersect",
            "into",
            "isolation",
            "json",
            "key",
            "kill",
            "language",
            "last",
            "legacy_cardinality_estimation",
            "level",
            "lineno",
            "load",
            "local",
            "locate",
            "location",
            "login",
            "masked",
            "master",
            "maxdop",
            "memory_optimized",
            "merge",
            "message",
            "modify",
            "move",
            "multi_user",
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
            "now",
            "numeric",
            "numeric_roundabort",
            "object",
            "of",
            "off",
            "offsets",
            "on",
            "online",
            "open",
            "opendatasource",
            "openquery",
            "openrowset",
            "openxml",
            "option",
            "order",
            "out",
            "output",
            "over",
            "owner",
            "pad_index",
            "page",
            "page_verify",
            "parameter_sniffing",
            "parameterization",
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
            "population",
            "precision",
            "predicate",
            "primary",
            "print",
            "prior",
            "proc",
            "procedure",
            "public",
            "query_optimizer_hotfixes",
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
            "replication",
            "required",
            "restart",
            "restore",
            "restrict",
            "resume",
            "return",
            "returns",
            "revert",
            "revoke",
            "role",
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
            "securityaudit",
            "select",
            "semantickeyphrasetable",
            "semanticsimilaritydetailstable",
            "semanticsimilaritytable",
            "send",
            "sent",
            "sequence",
            "server",
            "session",
            "set",
            "sets",
            "setuser",
            "shutdown",
            "simple",
            "smallint",
            "smallmoney",
            "snapshot",
            "sort_in_tempdb",
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
            "supported",
            "symmetric",
            "sysname",
            "system",
            "system_time",
            "system_versioning",
            "table",
            "tablesample",
            "take",
            "target",
            "textimage_on",
            "textsize",
            "then",
            "thesaurus",
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
            "trustworthy",
            "try",
            "tsql",
            "type",
            "union",
            "unique",
            "uniqueidentifier",
            "unlimited",
            "updatetext",
            "use",
            "user",
            "using",
            "value",
            "values",
            "varchar",
            "varying",
            "version",
            "view",
            "waitfor",
            "weight",
            "when",
            "where",
            "while",
            "with",
            "within",
            "within group",
            "without",
            "writetext",
            "xact_abort",
            "xml",
            "zone"
        };

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
            bool useLowerCase)
        {
            var completionItems = new CompletionItem[DefaultCompletionText.Length];
            for (int i = 0; i < DefaultCompletionText.Length; ++i)
            {
                completionItems[i] = CreateDefaultCompletionItem(
                    useLowerCase ? DefaultCompletionText[i].ToLower() : DefaultCompletionText[i].ToUpper(),
                    row, 
                    startColumn, 
                    endColumn);
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
            return new CompletionItem()
            {
                Label = label,
                Kind = CompletionItemKind.Keyword,
                Detail = label + " keyword",
                TextEdit = new TextEdit
                {
                    NewText = label,
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
                // convert the completion item candidates into CompletionItems
                completions.Add(new CompletionItem()
                {
                    Label = autoCompleteItem.Title,
                    Kind = CompletionItemKind.Variable,
                    Detail = autoCompleteItem.Title,
                    TextEdit = new TextEdit
                    {
                        NewText = autoCompleteItem.Title,
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
                });
            }

            return completions.ToArray();
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
