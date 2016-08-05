//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    internal class IntellisenseCache
    {
        // connection used to query for intellisense info
        private DbConnection connection;

        public IntellisenseCache(ISqlConnectionFactory connectionFactory, ConnectionDetails connectionDetails)
        {
            DatabaseInfo = CopySummary(connectionDetails);

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

            int i = 0;

            // Take a reference to the list at a point in time in case we update and replace the list
            var suggestions = AutoCompleteList;
            // the completion list will be null is user not connected to server
            if (this.AutoCompleteList != null)
            {

                foreach (var autoCompleteItem in suggestions)
                {
                    // convert the completion item candidates into CompletionItems
                    completions.Add(new CompletionItem()
                    {
                        Label = autoCompleteItem,
                        Kind = CompletionItemKind.Keyword,
                        Detail = autoCompleteItem + " details",
                        Documentation = autoCompleteItem + " documentation",
                        TextEdit = new TextEdit
                        {
                            NewText = autoCompleteItem,
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

        private static ConnectionSummary CopySummary(ConnectionSummary summary)
        {
            return new ConnectionSummary()
            {
                ServerName = summary.ServerName,
                DatabaseName = summary.DatabaseName,
                UserName = summary.UserName
            };
        }
    }

    /// <summary>
    /// Treats connections as the same if their server, db and usernames all match
    /// </summary>
    public class ConnectionSummaryComparer : IEqualityComparer<ConnectionSummary>
    {
        public bool Equals(ConnectionSummary x, ConnectionSummary y)
        {
            if (x == y) { return true; }
            else if (x != null)
            {
                if (y == null) { return false; }

                // Compare server, db, username. Note: server is case-insensitive in the driver
                return string.Compare(x.ServerName, y.ServerName, StringComparison.OrdinalIgnoreCase) == 0
                    && string.Compare(x.DatabaseName, y.DatabaseName, StringComparison.Ordinal) == 0
                    && string.Compare(x.UserName, y.UserName, StringComparison.Ordinal) == 0;
            }
            return false;
        }

        public int GetHashCode(ConnectionSummary obj)
        {
            int hashcode = 31;
            if (obj != null)
            {
                if (obj.ServerName != null)
                {
                    hashcode ^= obj.ServerName.GetHashCode();
                }
                if (obj.DatabaseName != null)
                {
                    hashcode ^= obj.DatabaseName.GetHashCode();
                }
                if (obj.UserName != null)
                {
                    hashcode ^= obj.UserName.GetHashCode();
                }
            }
            return hashcode;
        }
    }
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        #region Singleton Instance Implementation

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<AutoCompleteService> instance
            = new Lazy<AutoCompleteService>(() => new AutoCompleteService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static AutoCompleteService Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Default, parameterless constructor.
        /// TODO: Figure out how to make this truely singleton even with dependency injection for tests
        /// </summary>
        public AutoCompleteService()
        {
        }

        #endregion

        // Dictionary of unique intellisense caches for each Connection
        private Dictionary<ConnectionSummary, IntellisenseCache> caches =
            new Dictionary<ConnectionSummary, IntellisenseCache>(new ConnectionSummaryComparer());

        private ISqlConnectionFactory factory;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ISqlConnectionFactory ConnectionFactory
        {
            get
            {
                // TODO consider protecting against multi-threaded access
                if (factory == null)
                {
                    factory = new SqlConnectionFactory();
                }
                return factory;
            }
            set
            {
                factory = value;
            }
        }
        public void InitializeService(ServiceHost serviceHost)
        {
            // Register a callback for when a connection is created
            ConnectionService.Instance.RegisterOnConnectionTask(UpdateAutoCompleteCache);
        }

        private async Task UpdateAutoCompleteCache(ConnectionInfo connectionInfo)
        {
            if (connectionInfo != null)
            {
                await UpdateAutoCompleteCache(connectionInfo.ConnectionDetails);
            }
        }

        /// <summary>
        /// Update the cached autocomplete candidate list when the user connects to a database
        /// TODO: Update with refactoring/async
        /// </summary>
        /// <param name="details"></param>
        public async Task UpdateAutoCompleteCache(ConnectionDetails details)
        {
            IntellisenseCache cache;
            if (!caches.TryGetValue(details, out cache))
            {
                cache = new IntellisenseCache(ConnectionFactory, details);
                caches[cache.DatabaseInfo] = cache;
            }

            await cache.UpdateCache();
        }

        /// <summary>
        /// Return the completion item list for the current text position.
        /// This method does not await cache builds since it expects to return quickly
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        public CompletionItem[] GetCompletionItems(TextDocumentPosition textDocumentPosition)
        {
            // Try to find a cache for the document's backing connection (if available)
            // If we have a connection but no cache, we don't care - assuming the OnConnect and OnDisconnect listeners
            // behave well, there should be a cache for any actively connected document. This also helps skip documents
            // that are not backed by a SQL connection
            ConnectionInfo connectionInfo;
            IntellisenseCache cache;
            if (ConnectionService.Instance.TryFindConnection(textDocumentPosition.Uri, out connectionInfo)
                && caches.TryGetValue(connectionInfo.ConnectionDetails, out cache))
            {
                return cache.GetAutoCompleteItems(textDocumentPosition).ToArray();
            }

            return new CompletionItem[0];
        }

    }
}

