//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        Object syncLock = new Object();

        private ManualResetEvent metadataBuildingEvent = new ManualResetEvent(false);

        /// <summary>
        /// Event which tells if MetadataProvider is built fully or not
        /// </summary>
        public ManualResetEvent BuildEvent
        {
            get { return this.metadataBuildingEvent; }
        }

        private bool ShouldEnableAutocomplete()
        {
            return true;
        }

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
        /// Internal constructor for use in test cases only
        /// </summary>
        internal AutoCompleteService()
        {
        }

        #endregion

        private ConnectionService connectionService = null;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if(connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        public void InitializeService(ServiceHost serviceHost)
        {
            // Register auto-complete request handler
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);

            // Register a callback for when a connection is created
            ConnectionServiceInstance.RegisterOnConnectionTask(UpdateAutoCompleteCache);

            // Register a callback for when a connection is closed
            ConnectionServiceInstance.RegisterOnDisconnectTask(RemoveAutoCompleteCacheUriReference);
        }

         /// <summary>
         /// Auto-complete completion provider request callback
         /// </summary>
         /// <param name="textDocumentPosition"></param>
         /// <param name="requestContext"></param>
         /// <returns></returns>
         private static async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {
            // get the current list of completion items and return to client 
            var scriptFile = WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetFile(
                textDocumentPosition.TextDocument.Uri);

            ConnectionInfo connInfo;
            ConnectionService.Instance.TryFindConnection(
                scriptFile.ClientFilePath, 
                out connInfo);

            var completionItems = Instance.GetCompletionItems(
                textDocumentPosition, scriptFile, connInfo);

            await requestContext.SendResult(completionItems); 
        }

        /// <summary>
        /// Remove a reference to an autocomplete cache from a URI. If
        /// it is the last URI connected to a particular connection,
        /// then remove the cache.
        /// </summary>
        public async Task RemoveAutoCompleteCacheUriReference(ConnectionSummary summary)
        {
            // currently this method is disabled, but we need to reimplement now that the 
            // implementation of the 'cache' has changed.
            await Task.FromResult(0);
        }

        /// <summary>
        /// Update the cached autocomplete candidate list when the user connects to a database
        /// </summary>
        /// <param name="info"></param>
        public async Task UpdateAutoCompleteCache(ConnectionInfo info)
        {
            await Task.Run( () => 
            {
                if (ShouldEnableAutocomplete())
                {
                    lock (this.syncLock)
                    {
                        try
                        {
                            this.BuildEvent.Reset();

                            if (!LanguageService.Instance.ScriptParseInfoMap.ContainsKey(info.OwnerUri))
                            {
                                var sqlConn = info.SqlConnection as SqlConnection;
                                if (sqlConn != null)
                                {
                                    var srvConn = new ServerConnection(sqlConn);
                                    this.ServerVersion =srvConn.ServerVersion;
                                    this.DatabaseEngineType = srvConn.DatabaseEngineType;

                                    var displayInfoProvider = new MetadataDisplayInfoProvider();
                                    var metadataProvider = SmoMetadataProvider.CreateConnectedProvider(srvConn);
                                    var binder = BinderProvider.CreateBinder(metadataProvider);

                                    LanguageService.Instance.ScriptParseInfoMap.Add(info.OwnerUri,
                                        new ScriptParseInfo()
                                        {
                                            Binder = binder,
                                            MetadataProvider = metadataProvider,
                                            MetadataDisplayInfoProvider = displayInfoProvider
                                        });

                                    var scriptFile = WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetFile(info.OwnerUri);
                                    
                                    LanguageService.Instance.ParseAndBind(scriptFile, info);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            // Set Metadata Build event to Signal state.
                            // (Tell Language Service thread that I am ready with Metadata Provider Object)
                            this.BuildEvent.Set();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Find the position of the previous delimeter for autocomplete token replacement.
        /// SQL Parser may have similar functionality in which case we'll delete this method.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="startRow"></param>
        /// <param name="startColumn"></param>
        /// <returns></returns>
        private int PositionOfPrevDelimeter(string sql, int startRow, int startColumn)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return 1;
            }

            int prevLineColumns = 0;
            for (int i = 0; i < startRow; ++i)
            {
                while (sql[prevLineColumns] != '\n' && prevLineColumns < sql.Length)
                {
                    ++prevLineColumns;
                }
                ++prevLineColumns;
            }

            startColumn += prevLineColumns;

            if (startColumn - 1 < sql.Length)
            {
                while (--startColumn >= prevLineColumns)
                {
                    if (sql[startColumn] == ' ' 
                        || sql[startColumn] == '\t'
                        || sql[startColumn] == '\n'
                        || sql[startColumn] == '.'
                        || sql[startColumn] == '+'
                        || sql[startColumn] == '-'
                        || sql[startColumn] == '*'
                        || sql[startColumn] == '>'
                        || sql[startColumn] == '<'
                        || sql[startColumn] == '='
                        || sql[startColumn] == '/'
                        || sql[startColumn] == '%')
                    {
                        break;
                    }
                }
            }

            return startColumn + 1 - prevLineColumns;
        }

        /// <summary>
        /// Determines whether a reparse and bind is required to provide autocomplete
        /// </summary>
        /// <param name="info"></param>
        private bool RequiresReparse(ScriptParseInfo info, ScriptFile scriptFile)
        {
            string prevSqlText = info.ParseResult.Script.Sql;
            string currentSqlText = scriptFile.Contents;

            return prevSqlText.Length != currentSqlText.Length
                || !string.Equals(prevSqlText, currentSqlText);
        }

        /// <summary>
        /// Converts a list of Declaration objects to CompletionItem objects
        /// since VS Code expects CompletionItems but SQL Parser works with Declarations
        /// </summary>
        /// <param name="suggestions"></param>
        /// <param name="cursorRow"></param>
        /// <param name="cursorColumn"></param>
        /// <returns></returns>
        private CompletionItem[] ConvertDeclarationsToCompletionItems(
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
                    Kind = CompletionItemKind.Keyword,
                    Detail = autoCompleteItem.Title,
                    Documentation = autoCompleteItem.Description,
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
        /// Return the completion item list for the current text position.
        /// This method does not await cache builds since it expects to return quickly
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        public CompletionItem[] GetCompletionItems(
            TextDocumentPosition textDocumentPosition,
            ScriptFile scriptFile, 
            ConnectionInfo connInfo)
        {
            string filePath = textDocumentPosition.TextDocument.Uri;

            // Take a reference to the list at a point in time in case we update and replace the list
            if (connInfo == null 
                || !LanguageService.Instance.ScriptParseInfoMap.ContainsKey(textDocumentPosition.TextDocument.Uri))
            {
                return new CompletionItem[0];
            }

            // reparse and bind the SQL statement if needed
            var scriptParseInfo = LanguageService.Instance.ScriptParseInfoMap[textDocumentPosition.TextDocument.Uri];
            if (RequiresReparse(scriptParseInfo, scriptFile))
            {       
                LanguageService.Instance.ParseAndBind(scriptFile, connInfo);
            }

            if (scriptParseInfo.ParseResult == null)
            {
                return new CompletionItem[0];
            }

            // get the completion list from SQL Parser
            var suggestions = Resolver.FindCompletions(
                scriptParseInfo.ParseResult, 
                textDocumentPosition.Position.Line + 1, 
                textDocumentPosition.Position.Character + 1, 
                scriptParseInfo.MetadataDisplayInfoProvider); 

            // convert the suggestion list to the VS Code format
            return ConvertDeclarationsToCompletionItems(
                suggestions, 
                textDocumentPosition.Position.Line,
                PositionOfPrevDelimeter(
                    scriptFile.Contents, 
                    textDocumentPosition.Position.Line,
                    textDocumentPosition.Position.Character),
                textDocumentPosition.Position.Character);
        }




    }
}
