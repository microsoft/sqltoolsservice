//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    internal class IntellisenseCache
    {
        /// <summary>
        /// connection used to query for intellisense info
        /// </summary>
        private DbConnection connection;

        /// <summary>
        /// Number of documents (URI's) that are using the cache for the same database.
        /// The autocomplete service uses this to remove unreferenced caches.
        /// </summary>
        public int ReferenceCount { get; set; }

        public IntellisenseCache(ISqlConnectionFactory connectionFactory, ConnectionDetails connectionDetails)
        {
            ReferenceCount = 0;
            DatabaseInfo = connectionDetails.Clone();

            // TODO error handling on this. Intellisense should catch or else the service should handle
            connection = connectionFactory.CreateSqlConnection(ConnectionService.BuildConnectionString(connectionDetails));
            connection.Open();
        }

        /// <summary>
        /// Used to identify a database for which this cache is used
        /// </summary>
        public ConnectionSummary DatabaseInfo
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the current autocomplete candidate list
        /// </summary>
        public IEnumerable<string> AutoCompleteList { get; private set; }

        public async Task UpdateCache()
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.tables";
            command.CommandTimeout = 15;
            command.CommandType = CommandType.Text;
            var reader = await command.ExecuteReaderAsync();

            List<string> results = new List<string>();
            while (await reader.ReadAsync())
            {
                results.Add(reader[0].ToString());
            }

            AutoCompleteList = results;
            await Task.FromResult(0);
        }

        public List<CompletionItem> GetAutoCompleteItems(TextDocumentPosition textDocumentPosition)
        {
            List<CompletionItem> completions = new List<CompletionItem>();

            // Take a reference to the list at a point in time in case we update and replace the list
            //var suggestions = AutoCompleteList;
            if (!LanguageService.Instance.ScriptParseInfoMap.ContainsKey(textDocumentPosition.TextDocument.Uri))
            {
                return completions;
            }

            var scriptParseInfo = LanguageService.Instance.ScriptParseInfoMap[textDocumentPosition.TextDocument.Uri];
            var suggestions = Resolver.FindCompletions(
                scriptParseInfo.ParseResult, 
                textDocumentPosition.Position.Line + 1, 
                textDocumentPosition.Position.Character + 1, 
                scriptParseInfo.MetadataDisplayInfoProvider); 

            int i = 0;

            // the completion list will be null is user not connected to server
            if (this.AutoCompleteList != null)
            {

                foreach (var autoCompleteItem in suggestions)
                {
                    // convert the completion item candidates into CompletionItems
                    completions.Add(new CompletionItem()
                    {
                        Label = autoCompleteItem.Title,
                        Kind = CompletionItemKind.Keyword,
                        Detail = autoCompleteItem.Title + " details",
                        Documentation = autoCompleteItem.Title + " documentation",
                        TextEdit = new TextEdit
                        {
                            NewText = autoCompleteItem.Title,
                            Range = new Range
                            {
                                Start = new Position
                                {
                                    Line = textDocumentPosition.Position.Line,
                                    Character = textDocumentPosition.Position.Character
                                },
                                End = new Position
                                {
                                    Line = textDocumentPosition.Position.Line,
                                    Character = textDocumentPosition.Position.Character + 5
                                }
                            }
                        }
                    });

                    // only show 50 items
                    if (++i == 50)
                    {
                        break;
                    }
                }
            }

            return completions;
        }
    }
}
