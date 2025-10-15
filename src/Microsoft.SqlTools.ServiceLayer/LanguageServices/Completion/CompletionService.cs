//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.Utility;


namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion
{
    /// <summary>
    /// A service to create auto complete list for given script document 
    /// </summary>
    internal class CompletionService
    {
        private ConnectedBindingQueue BindingQueue { get; set; }

        /// <summary>
        /// Created new instance given binding queue
        /// </summary>
        public CompletionService(ConnectedBindingQueue bindingQueue)
        {
            BindingQueue = bindingQueue;
        }

        private ISqlParserWrapper sqlParserWrapper;

        /// <summary>
        /// SQL parser wrapper to create the completion list
        /// </summary>
        public ISqlParserWrapper SqlParserWrapper
        {
            get
            {
                this.sqlParserWrapper ??= new SqlParserWrapper();
                return this.sqlParserWrapper;
            }
            set
            {
                this.sqlParserWrapper = value;
            }
        }
        private const int CrossSchemaLookupLimit = 200;

        private const string CrossSchemaLookupQuery = @"WITH candidates AS (
    SELECT s.name AS SchemaName, o.name AS ObjectName, o.type AS ObjectType
    FROM sys.objects o
    JOIN sys.schemas s ON o.schema_id = s.schema_id
    WHERE o.parent_object_id = 0
      AND o.type IN ('AF','FN','FS','FT','IF','P','PC','RF','SQ','TF','U','V','X')
      AND o.name LIKE @pattern ESCAPE '\'
    UNION ALL
    SELECT s.name, sn.name, 'SN'
    FROM sys.synonyms sn
    JOIN sys.schemas s ON sn.schema_id = s.schema_id
    WHERE sn.name LIKE @pattern ESCAPE '\'
)
SELECT DISTINCT TOP (@limit)
    SchemaName,
    ObjectName,
    ObjectType
FROM candidates
ORDER BY ObjectName, SchemaName;";


        /// <summary>
        /// Creates a completion list given connection and document info
        /// </summary>
        public AutoCompletionResult CreateCompletions(
            ConnectionInfo connInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            // check if the file is connected and the file lock is available
            if (scriptDocumentInfo.ScriptParseInfo.IsConnected && Monitor.TryEnter(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock))
            {
                try
                {
                    QueueItem queueItem = AddToQueue(connInfo, scriptDocumentInfo.ScriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);

                    // wait for the queue item
                    queueItem.ItemProcessed.WaitOne();
                    var completionResult = queueItem.GetResultAsT<AutoCompletionResult>();
                    if (completionResult != null && completionResult.CompletionItems != null && completionResult.CompletionItems.Length > 0)
                    {
                        result = completionResult;
                    }
                    else if (!ShouldShowCompletionList(scriptDocumentInfo.Token))
                    {
                        result.CompleteResult(AutoCompleteHelper.EmptyCompletionList);
                    }
                }
                finally
                {
                    Monitor.Exit(scriptDocumentInfo.ScriptParseInfo.BuildingMetadataLock);
                }
            }

            return result;
        }

        private QueueItem AddToQueue(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            bool useLowerCaseSuggestions)
        {
            // queue the completion task with the binding queue    
            QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                key: scriptParseInfo.ConnectionKey,
                bindingTimeout: LanguageService.BindingTimeout,
                bindOperation: (bindingContext, cancelToken) =>
                {
                    return CreateCompletionsFromSqlParser(connInfo, scriptParseInfo, scriptDocumentInfo, bindingContext);
                },
                timeoutOperation: (bindingContext) =>
                {
                    // return the default list if the connected bind fails
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                },
                errorHandler: ex =>
                {
                    // return the default list if an unexpected exception occurs
                    return CreateDefaultCompletionItems(scriptParseInfo, scriptDocumentInfo, useLowerCaseSuggestions);
                });
            return queueItem;
        }

        private static bool ShouldShowCompletionList(Token token)
        {
            bool result = true;
            if (token != null)
            {
                switch (token.Id)
                {
                    case (int)Tokens.LEX_MULTILINE_COMMENT:
                    case (int)Tokens.LEX_END_OF_LINE_COMMENT:
                        result = false;
                        break;
                }
            }
            return result;
        }

        private AutoCompletionResult CreateDefaultCompletionItems(ScriptParseInfo scriptParseInfo, ScriptDocumentInfo scriptDocumentInfo, bool useLowerCaseSuggestions)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            CompletionItem[] completionList = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, useLowerCaseSuggestions);
            result.CompleteResult(completionList);
            return result;
        }

        private AutoCompletionResult CreateCompletionsFromSqlParser(
            ConnectionInfo connInfo,
            ScriptParseInfo scriptParseInfo,
            ScriptDocumentInfo scriptDocumentInfo,
            IBindingContext bindingContext)
        {
            AutoCompletionResult result = new AutoCompletionResult();
            MetadataDisplayInfoProvider displayInfoProvider = bindingContext?.MetadataDisplayInfoProvider ?? new MetadataDisplayInfoProvider();

            IEnumerable<Declaration> suggestions = SqlParserWrapper.FindCompletions(
                scriptParseInfo.ParseResult,
                scriptDocumentInfo.ParserLine,
                scriptDocumentInfo.ParserColumn,
                displayInfoProvider);

            scriptParseInfo.CurrentSuggestions = suggestions;

            CompletionItem[] completionList = AutoCompleteHelper.ConvertDeclarationsToCompletionItems(
                scriptParseInfo.CurrentSuggestions,
                scriptDocumentInfo.StartLine,
                scriptDocumentInfo.StartColumn,
                scriptDocumentInfo.EndColumn,
                scriptDocumentInfo.TokenText);

            completionList = AugmentWithCrossSchemaSuggestions(completionList, scriptParseInfo, scriptDocumentInfo, bindingContext);

            result.CompleteResult(completionList);

            connInfo.IntellisenseMetrics.UpdateMetrics(result.Duration, 1, (k2, v2) => v2 + 1);
            return result;
        }
        private CompletionItem[] AugmentWithCrossSchemaSuggestions(CompletionItem[] existingItems, ScriptParseInfo scriptParseInfo, ScriptDocumentInfo scriptDocumentInfo, IBindingContext bindingContext)
        {
            if (existingItems == null || existingItems.Length == 0 || bindingContext == null)
            {
                return existingItems ?? Array.Empty<CompletionItem>();
            }

            if (scriptDocumentInfo?.Contents == null || IsSchemaQualified(scriptDocumentInfo))
            {
                return existingItems;
            }

            string tokenText = scriptDocumentInfo.TokenText ?? string.Empty;
            string searchPrefix = SqlCompletionItem.NormalizeIdentifier(SqlCompletionItem.GetUnqualifiedName(tokenText));
            if (string.IsNullOrWhiteSpace(searchPrefix))
            {
                return existingItems;
            }

            if (searchPrefix.Length > 128)
            {
                searchPrefix = searchPrefix.Substring(0, 128);
            }

            var sqlConnection = bindingContext.ServerConnection?.SqlConnectionObject;
            if (sqlConnection == null)
            {
                return existingItems;
            }

            List<CompletionItem> additionalItems = LookupCrossSchemaObjects(sqlConnection, searchPrefix, existingItems, scriptDocumentInfo);
            if (additionalItems.Count == 0)
            {
                return existingItems;
            }

            return existingItems.Concat(additionalItems).ToArray();
        }

        private static bool IsSchemaQualified(ScriptDocumentInfo documentInfo)
        {
            if (documentInfo == null || documentInfo.Contents == null)
            {
                return false;
            }

            int tokenStartIndex = GetTokenStartIndex(documentInfo);
            if (tokenStartIndex <= 0)
            {
                return false;
            }

            int index = tokenStartIndex - 1;
            string contents = documentInfo.Contents;
            while (index >= 0 && char.IsWhiteSpace(contents[index]))
            {
                index--;
            }

            return index >= 0 && contents[index] == '.';
        }

        private static int GetTokenStartIndex(ScriptDocumentInfo documentInfo)
        {
            if (documentInfo == null || documentInfo.Contents == null)
            {
                return -1;
            }

            return TextUtilities.PositionOfCursor(documentInfo.Contents, documentInfo.StartLine, documentInfo.StartColumn, out _);
        }

        private List<CompletionItem> LookupCrossSchemaObjects(SqlConnection sqlConnection, string searchPrefix, CompletionItem[] existingItems, ScriptDocumentInfo scriptDocumentInfo)
        {
            var results = new List<CompletionItem>();

            var existingLabels = new HashSet<string>(existingItems.Select(item => item.Label), StringComparer.OrdinalIgnoreCase);
            var existingInsertTexts = new HashSet<string>(existingItems.Select(item => item.InsertText), StringComparer.OrdinalIgnoreCase);

            string escapedPattern = EscapeLikePattern(searchPrefix) + "%";
            bool shouldCloseConnection = false;

            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                    shouldCloseConnection = true;
                }

                using (SqlCommand command = sqlConnection.CreateCommand())
                {
                    command.CommandText = CrossSchemaLookupQuery;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 5;
                    command.Parameters.Add(new SqlParameter("@pattern", SqlDbType.NVarChar, 256) { Value = escapedPattern });
                    command.Parameters.Add(new SqlParameter("@limit", SqlDbType.Int) { Value = CrossSchemaLookupLimit });

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string schemaName = reader.GetString(0);
                            string objectName = reader.GetString(1);
                            string objectType = reader.GetString(2);

                            string label = string.Concat(schemaName, '.', objectName);
                            string insertText = string.Concat(Utils.MakeSqlBracket(schemaName), '.', Utils.MakeSqlBracket(objectName));

                            if (existingLabels.Contains(label) || existingInsertTexts.Contains(insertText))
                            {
                                continue;
                            }

                            string detail = string.Concat(label, " (", GetObjectTypeDescription(objectType), ")");
                            CompletionItem item = SqlCompletionItem.CreateCompletionItem(
                                label,
                                detail,
                                insertText,
                                MapCompletionKind(objectType),
                                scriptDocumentInfo.StartLine,
                                scriptDocumentInfo.StartColumn,
                                scriptDocumentInfo.EndColumn);

                            item.FilterText = SqlCompletionItem.BuildFilterText(label);
                            item.SortText = SqlCompletionItem.NormalizeIdentifier(SqlCompletionItem.GetUnqualifiedName(label));

                            results.Add(item);
                            existingLabels.Add(label);
                            existingInsertTexts.Add(insertText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Verbose($"Cross-schema completion augmentation failed: {ex}");
            }
            finally
            {
                if (shouldCloseConnection && sqlConnection.State == ConnectionState.Open)
                {
                    sqlConnection.Close();
                }
            }

            return results;
        }

        private static string EscapeLikePattern(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_")
                .Replace("[", @"\[");
        }

        private static CompletionItemKind MapCompletionKind(string objectType)
        {
            if (string.IsNullOrEmpty(objectType))
            {
                return CompletionItemKind.Text;
            }

            switch (objectType)
            {
                case "U":
                case "V":
                    return CompletionItemKind.File;
                case "P":
                case "PC":
                case "RF":
                case "X":
                    return CompletionItemKind.Function;
                case "FN":
                case "FS":
                case "FT":
                case "IF":
                case "TF":
                case "AF":
                    return CompletionItemKind.Function;
                case "SQ":
                    return CompletionItemKind.Value;
                case "SN":
                    return CompletionItemKind.Reference;
                default:
                    return CompletionItemKind.Unit;
            }
        }

        private static string GetObjectTypeDescription(string objectType)
        {
            return objectType switch
            {
                "U" => "Table",
                "V" => "View",
                "P" => "Stored Procedure",
                "PC" => "CLR Stored Procedure",
                "RF" => "Replication Filter",
                "X" => "Extended Procedure",
                "FN" => "Scalar Function",
                "FS" => "CLR Scalar Function",
                "FT" => "CLR Table Function",
                "IF" => "Inline Function",
                "TF" => "Table Function",
                "AF" => "Aggregate Function",
                "SQ" => "Sequence",
                "SN" => "Synonym",
                _ => objectType
            };
        }
    }
}

