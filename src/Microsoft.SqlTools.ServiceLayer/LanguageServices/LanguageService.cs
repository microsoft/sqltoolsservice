//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that requires knowledge of
    /// the language to perform, such as definitions, intellisense, etc.
    /// </summary>
    public sealed class LanguageService
    {
        private const int ONE_SECOND = 1000;
        
        internal const string DefaultBatchSeperator = "GO";

        internal const int DiagnosticParseDelay = 750;

        internal const int HoverTimeout = 500;

        internal const int BindingTimeout = 500;

        internal const int OnConnectionWaitTimeout = 300 * ONE_SECOND;

        internal const int PeekDefinitionTimeout = 10 * ONE_SECOND;

        private static ConnectionService connectionService = null;

        private static WorkspaceService<SqlToolsSettings> workspaceServiceInstance;

        private object parseMapLock = new object();

        private ScriptParseInfo currentCompletionParseInfo;

        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();

        private ParseOptions defaultParseOptions =  new ParseOptions(
            batchSeparator: LanguageService.DefaultBatchSeperator,
            isQuotedIdentifierSet: true,  
            compatibilityLevel: DatabaseCompatibilityLevel.Current, 
            transactSqlVersion: TransactSqlVersion.Current);

        /// <summary>
        /// Gets or sets the binding queue instance
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectedBindingQueue BindingQueue
        {
            get
            {
                return this.bindingQueue;
            }
            set
            {
                this.bindingQueue = value;
            }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
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

        #region Singleton Instance Implementation

        private static readonly Lazy<LanguageService> instance = new Lazy<LanguageService>(() => new LanguageService());

        private Lazy<Dictionary<string, ScriptParseInfo>> scriptParseInfoMap 
            = new Lazy<Dictionary<string, ScriptParseInfo>>(() => new Dictionary<string, ScriptParseInfo>());

        /// <summary>
        /// Gets a mapping dictionary for SQL file URIs to ScriptParseInfo objects
        /// </summary>
        internal Dictionary<string, ScriptParseInfo> ScriptParseInfoMap 
        {
            get
            {
                return this.scriptParseInfoMap.Value;
            }
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static LanguageService Instance
        {
            get { return instance.Value; }
        }

        private ParseOptions DefaultParseOptions
        {
            get
            {
                return this.defaultParseOptions;
            }
        }

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal LanguageService()
        {
        }

        #endregion

        #region Properties

        private static CancellationTokenSource ExistingRequestCancellation { get; set; }

        /// <summary>
        /// Gets the current settings
        /// </summary>
        internal SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
        }

        /// <summary>
        /// Gets or sets the current workspace service instance
        /// Setter for internal testing purposes only
        /// </summary>
        internal static WorkspaceService<SqlToolsSettings> WorkspaceServiceInstance
        {
            get
            {
                if (LanguageService.workspaceServiceInstance == null)
                {
                    LanguageService.workspaceServiceInstance =  WorkspaceService<SqlToolsSettings>.Instance;
                }
                return LanguageService.workspaceServiceInstance;
            }
            set
            {
                LanguageService.workspaceServiceInstance = value;
            }
        }

        /// <summary>
        /// Gets the current workspace instance
        /// </summary>
        internal Workspace.Workspace CurrentWorkspace
        {
            get { return LanguageService.WorkspaceServiceInstance.Workspace; }
        }

        /// <summary>
        /// Gets or sets the current SQL Tools context
        /// </summary>
        /// <returns></returns>
        internal SqlToolsContext Context { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the Language Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost, SqlToolsContext context)
        {
            // Register the requests that this service will handle

            // turn off until needed (10/28/2016)
            // serviceHost.SetRequestHandler(ReferencesRequest.Type, HandleReferencesRequest);
            // serviceHost.SetRequestHandler(SignatureHelpRequest.Type, HandleSignatureHelpRequest);
            // serviceHost.SetRequestHandler(DocumentHighlightRequest.Type, HandleDocumentHighlightRequest);

            serviceHost.SetRequestHandler(CompletionResolveRequest.Type, HandleCompletionResolveRequest);
            serviceHost.SetRequestHandler(HoverRequest.Type, HandleHoverRequest);
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);
            serviceHost.SetRequestHandler(DefinitionRequest.Type, HandleDefinitionRequest);

            // Register a no-op shutdown task for validation of the shutdown logic
            serviceHost.RegisterShutdownTask(async (shutdownParams, shutdownRequestContext) =>
            {
                Logger.Write(LogLevel.Verbose, "Shutting down language service");
                await Task.FromResult(0);
            });

            // Register the configuration update handler
            WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);

            // Register the file change update handler
            WorkspaceService<SqlToolsSettings>.Instance.RegisterTextDocChangeCallback(HandleDidChangeTextDocumentNotification);

            // Register the file open update handler
            WorkspaceService<SqlToolsSettings>.Instance.RegisterTextDocOpenCallback(HandleDidOpenTextDocumentNotification);

            // Register a callback for when a connection is created
            ConnectionServiceInstance.RegisterOnConnectionTask(UpdateLanguageServiceOnConnection);

            // Register a callback for when a connection is closed
            ConnectionServiceInstance.RegisterOnDisconnectTask(RemoveAutoCompleteCacheUriReference);

            // Store the SqlToolsContext for future use
            Context = context;
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// Auto-complete completion provider request callback
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal static async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {
            // check if Intellisense suggestions are enabled
            if (!WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsSuggestionsEnabled)
            {
                await Task.FromResult(true);
            }
            else
            {                
                // get the current list of completion items and return to client 
                var scriptFile = LanguageService.WorkspaceServiceInstance.Workspace.GetFile(
                    textDocumentPosition.TextDocument.Uri);

                ConnectionInfo connInfo;
                LanguageService.ConnectionServiceInstance.TryFindConnection(
                    scriptFile.ClientFilePath, 
                    out connInfo);

                var completionItems = Instance.GetCompletionItems(
                    textDocumentPosition, scriptFile, connInfo);
               
                   await requestContext.SendResult(completionItems);
                }  
            }

        /// <summary>
        /// Handle the resolve completion request event to provide additional
        /// autocomplete metadata to the currently select completion item
        /// </summary>
        /// <param name="completionItem"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        private static async Task HandleCompletionResolveRequest(
            CompletionItem completionItem,
            RequestContext<CompletionItem> requestContext)
        {
            // check if Intellisense suggestions are enabled
            if (!WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsSuggestionsEnabled)
            {
                await Task.FromResult(true);
            }
            else
            {
                completionItem = LanguageService.Instance.ResolveCompletionItem(completionItem);
                await requestContext.SendResult(completionItem);
            }
        }

        internal static async Task HandleDefinitionRequest(TextDocumentPosition textDocumentPosition, RequestContext<Location[]> requestContext)
        {
            if (WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsIntellisenseEnabled)
            {
                // Retrieve document and connection
                ConnectionInfo connInfo;
                var scriptFile = LanguageService.WorkspaceServiceInstance.Workspace.GetFile(textDocumentPosition.TextDocument.Uri);
                LanguageService.ConnectionServiceInstance.TryFindConnection(scriptFile.ClientFilePath, out connInfo);

                Location[] locations = LanguageService.Instance.GetDefinition(textDocumentPosition, scriptFile, connInfo);
                if (locations != null)
                {
                    await requestContext.SendResult(locations);
                }
            }
        }

// turn off this code until needed (10/28/2016)
#if false
        private static async Task HandleReferencesRequest(
            ReferencesParams referencesParams,
            RequestContext<Location[]> requestContext)
        {
            await Task.FromResult(true);
        }

        private static async Task HandleSignatureHelpRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<SignatureHelp> requestContext)
        {
            await Task.FromResult(true);
        }

        private static async Task HandleDocumentHighlightRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<DocumentHighlight[]> requestContext)
        {
            await Task.FromResult(true);
        }
#endif

        private static async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {                    
            // check if Quick Info hover tooltips are enabled
            if (WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsQuickInfoEnabled)
            {        
                var scriptFile = WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetFile(
                    textDocumentPosition.TextDocument.Uri);

                var hover = LanguageService.Instance.GetHoverItem(textDocumentPosition, scriptFile);
                if (hover != null)
                {
                    await requestContext.SendResult(hover);
                }
            }

            await requestContext.SendResult(new Hover());         
        }

        #endregion

        #region Handlers for Events from Other Services

        /// <summary>
        /// Handle the file open notification 
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        public async Task HandleDidOpenTextDocumentNotification(
            ScriptFile scriptFile, 
            EventContext eventContext)
        {
            // if not in the preview window and diagnostics are enabled then run diagnostics
            if (!IsPreviewWindow(scriptFile)
                && WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsDiagnositicsEnabled)
            {
                await RunScriptDiagnostics( 
                    new ScriptFile[] { scriptFile },
                    eventContext); 
            }

            await Task.FromResult(true);
        }
        
        /// <summary> 
        /// Handles text document change events 
        /// </summary> 
        /// <param name="textChangeParams"></param> 
        /// <param name="eventContext"></param>
        public async Task HandleDidChangeTextDocumentNotification(ScriptFile[] changedFiles, EventContext eventContext) 
        { 
            if (WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings.IsDiagnositicsEnabled)
            {
                await this.RunScriptDiagnostics( 
                    changedFiles.ToArray(), 
                    eventContext); 
            }

            await Task.FromResult(true); 
        }

        /// <summary>
        /// Handle the file configuration change notification
        /// </summary>
        /// <param name="newSettings"></param>
        /// <param name="oldSettings"></param>
        /// <param name="eventContext"></param>
        public async Task HandleDidChangeConfigurationNotification(
            SqlToolsSettings newSettings, 
            SqlToolsSettings oldSettings, 
            EventContext eventContext)
        {
            bool oldEnableIntelliSense = oldSettings.SqlTools.IntelliSense.EnableIntellisense;
            bool? oldEnableDiagnostics = oldSettings.SqlTools.IntelliSense.EnableErrorChecking;

            // update the current settings to reflect any changes
            CurrentSettings.Update(newSettings);

            // if script analysis settings have changed we need to clear the current diagnostic markers
            if (oldEnableIntelliSense != newSettings.SqlTools.IntelliSense.EnableIntellisense
                || oldEnableDiagnostics != newSettings.SqlTools.IntelliSense.EnableErrorChecking)
            {
                // if the user just turned off diagnostics then send an event to clear the error markers
                if (!newSettings.IsDiagnositicsEnabled)
                {
                    ScriptFileMarker[] emptyAnalysisDiagnostics = new ScriptFileMarker[0];

                    foreach (var scriptFile in WorkspaceService<SqlToolsSettings>.Instance.Workspace.GetOpenedFiles())
                    {
                        await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, emptyAnalysisDiagnostics, eventContext);
                    }
                }
                // otherwise rerun diagnostic analysis on all opened SQL files
                else
                {
                    await this.RunScriptDiagnostics(CurrentWorkspace.GetOpenedFiles(), eventContext);
                }
            }
        }
        
        #endregion


        #region "AutoComplete Provider methods"

        /// <summary>
        /// Remove a reference to an autocomplete cache from a URI. If
        /// it is the last URI connected to a particular connection,
        /// then remove the cache.
        /// </summary>
        public async Task RemoveAutoCompleteCacheUriReference(ConnectionSummary summary, string ownerUri)
        {
            RemoveScriptParseInfo(ownerUri);

            // currently this method is disabled, but we need to reimplement now that the 
            // implementation of the 'cache' has changed.
            await Task.FromResult(0);
        }

        /// <summary>
        /// Parses the SQL text and binds it to the SMO metadata provider if connected 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="sqlText"></param>
        /// <returns>The ParseResult instance returned from SQL Parser</returns>
        public ParseResult ParseAndBind(ScriptFile scriptFile, ConnectionInfo connInfo)
        {
            // get or create the current parse info object
            ScriptParseInfo parseInfo = GetScriptParseInfo(scriptFile.ClientFilePath, createIfNotExists: true);

            if (Monitor.TryEnter(parseInfo.BuildingMetadataLock, LanguageService.BindingTimeout))
            {
                try
                {
                    if (connInfo == null || !parseInfo.IsConnected)
                    {
                        // parse current SQL file contents to retrieve a list of errors
                        ParseResult parseResult = Parser.IncrementalParse(
                            scriptFile.Contents,
                            parseInfo.ParseResult,
                            this.DefaultParseOptions);

                        parseInfo.ParseResult = parseResult;
                    }
                    else
                    {
                        QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                            key: parseInfo.ConnectionKey,
                            bindingTimeout: LanguageService.BindingTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {                          
                                try
                                {
                                    ParseResult parseResult = Parser.IncrementalParse(
                                        scriptFile.Contents,
                                        parseInfo.ParseResult,
                                        bindingContext.ParseOptions);

                                    parseInfo.ParseResult = parseResult;

                                    List<ParseResult> parseResults = new List<ParseResult>();
                                    parseResults.Add(parseResult);
                                    bindingContext.Binder.Bind(
                                        parseResults, 
                                        connInfo.ConnectionDetails.DatabaseName, 
                                        BindMode.Batch);
                                }
                                catch (ConnectionException)
                                {
                                    Logger.Write(LogLevel.Error, "Hit connection exception while binding - disposing binder object...");
                                }
                                catch (SqlParserInternalBinderError)
                                {
                                    Logger.Write(LogLevel.Error, "Hit connection exception while binding - disposing binder object...");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write(LogLevel.Error, "Unknown exception during parsing " + ex.ToString());
                                }

                                return null;
                            });            

                            queueItem.ItemProcessed.WaitOne();                      
                    }
                }
                catch (Exception ex)
                {
                    // reset the parse result to do a full parse next time
                    parseInfo.ParseResult = null;
                    Logger.Write(LogLevel.Error, "Unknown exception during parsing " + ex.ToString());
                }
                finally
                {
                    Monitor.Exit(parseInfo.BuildingMetadataLock);
                }
            }
            else
            {
               Logger.Write(LogLevel.Warning, "Binding metadata lock timeout in ParseAndBind"); 
            }
    
            return parseInfo.ParseResult;
        }

        /// <summary>
        /// Update the autocomplete metadata provider when the user connects to a database
        /// </summary>
        /// <param name="info"></param>
        public async Task UpdateLanguageServiceOnConnection(ConnectionInfo info)
        {
            await Task.Run(() => 
            {
                ScriptParseInfo scriptInfo = GetScriptParseInfo(info.OwnerUri, createIfNotExists: true);
                if (Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {                                                                      
                        scriptInfo.ConnectionKey = this.BindingQueue.AddConnectionContext(info);
                        scriptInfo.IsConnected = true;
                        
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(LogLevel.Error, "Unknown error in OnConnection " + ex.ToString());
                        scriptInfo.IsConnected = false;
                    }
                    finally
                    {
                        // Set Metadata Build event to Signal state.
                        // (Tell Language Service that I am ready with Metadata Provider Object)
                        Monitor.Exit(scriptInfo.BuildingMetadataLock);
                    }
                }  

                AutoCompleteHelper.PrepopulateCommonMetadata(info, scriptInfo, this.BindingQueue);

                // Send a notification to signal that autocomplete is ready
                ServiceHost.Instance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = info.OwnerUri});
            });
        }

        /// <summary>
        /// Determines whether a reparse and bind is required to provide autocomplete
        /// </summary>
        /// <param name="info"></param>
        private bool RequiresReparse(ScriptParseInfo info, ScriptFile scriptFile)
        {
            if (info.ParseResult == null)
            {
                return true;
            }

            string prevSqlText = info.ParseResult.Script.Sql;
            string currentSqlText = scriptFile.Contents;

            return prevSqlText.Length != currentSqlText.Length
                || !string.Equals(prevSqlText, currentSqlText);
        }

        /// <summary>
        /// Resolves the details and documentation for a completion item
        /// </summary>
        /// <param name="completionItem"></param>
        internal CompletionItem ResolveCompletionItem(CompletionItem completionItem)
        {
            var scriptParseInfo = LanguageService.Instance.currentCompletionParseInfo;
            if (scriptParseInfo != null && scriptParseInfo.CurrentSuggestions != null)
            {
                if (Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
                {
                    try
                    {
                        QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                            key: scriptParseInfo.ConnectionKey,
                            bindingTimeout: LanguageService.BindingTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {                                                          
                                foreach (var suggestion in scriptParseInfo.CurrentSuggestions)
                                {
                                    if (string.Equals(suggestion.Title, completionItem.Label))
                                    {
                                        completionItem.Detail = suggestion.DatabaseQualifiedName;
                                        completionItem.Documentation = suggestion.Description;
                                        break;
                                    }
                                }  
                                return completionItem;                             
                            });

                        queueItem.ItemProcessed.WaitOne();  
                    }
                    catch (Exception ex)
                    {
                        // if any exceptions are raised looking up extended completion metadata 
                        // then just return the original completion item
                        Logger.Write(LogLevel.Error, "Exeception in ResolveCompletionItem " + ex.ToString());
                    } 
                    finally
                    {
                       Monitor.Exit(scriptParseInfo.BuildingMetadataLock); 
                    }      
                }
            }
                

            return completionItem;
        }

        /// <summary>
        /// Get definition for a selected sql object using SMO Scripting
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptFile"></param>
        /// <param name="connInfo"></param>
        /// <returns> Location with the URI of the script file</returns>
        internal Location[] GetDefinition(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ConnectionInfo connInfo)
        {
            // Parse sql
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);
            if (scriptParseInfo != null && RequiresReparse(scriptParseInfo, scriptFile))
            {
                ParseAndBind(scriptFile, connInfo);
            }

            // Return if parse failed
            if (scriptParseInfo == null)
            {
                return null;
            }

            // Get token from selected text
            Token selectedToken = GetToken(scriptParseInfo, textDocumentPosition.Position.Line + 1, textDocumentPosition.Position.Character);
            if (selectedToken == null)
            {
                return null;
            }
            // Strip "[" and "]"(if present) from the token text to enable matching with the suggestions.
            // The suggestion title does not contain any sql punctuation
            string tokenText = TextUtilities.RemoveSquareBracketSyntax(selectedToken.Text);

            if (scriptParseInfo.IsConnected && Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
            {
                try
                {
                    // Queue the task with the binding queue    
                    QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                        key: scriptParseInfo.ConnectionKey,
                        bindingTimeout: LanguageService.PeekDefinitionTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            // Get suggestions for the token
                            int parserLine = textDocumentPosition.Position.Line + 1;
                            int parserColumn = textDocumentPosition.Position.Character + 1;
                            IEnumerable<Declaration> declarationItems = Resolver.FindCompletions(
                                scriptParseInfo.ParseResult, 
                                parserLine, parserColumn, 
                                bindingContext.MetadataDisplayInfoProvider);

                            // Match token with the suggestions(declaration items) returned
                            string schemaName = this.GetSchemaName(scriptParseInfo, textDocumentPosition.Position, scriptFile);
                            PeekDefinition peekDefinition = new PeekDefinition(connInfo);
                            return peekDefinition.GetScript(declarationItems, tokenText, schemaName);
                            

                        },
                        timeoutOperation: (bindingContext) =>
                        {
                            return null;
                        });

                    // wait for the queue item
                    queueItem.ItemProcessed.WaitOne();
                    return queueItem.GetResultAsT<Location[]>();
                }
                finally
                {
                    Monitor.Exit(scriptParseInfo.BuildingMetadataLock);
                }
            }

            return null;
        }

        /// <summary>
        /// Extract schema name for a token, if present
        /// </summary>
        /// <param name="scriptParseInfo"></param>
        /// <param name="position"></param>
        /// <param name="scriptFile"></param>
        /// <returns> schema nama</returns>
        private string GetSchemaName(ScriptParseInfo scriptParseInfo, Position position, ScriptFile scriptFile)
        {
            // Get schema name 
            string sql = scriptFile.GetLine(position.Line + 1);
            int startColumn = TextUtilities.PositionOfPrevDelimeter(sql, 0, position.Character);
            // if no schema name, returns null
            if( sql[startColumn - 1] != '.')
            {
                return null;
            }
            return TextUtilities.RemoveSquareBracketSyntax(GetToken(scriptParseInfo, position.Line + 1, startColumn - 1).Text);
        }


        /// <summary>
        /// Get quick info hover tooltips for the current position
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptFile"></param>
        internal Hover GetHoverItem(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile)
        {
            int startLine = textDocumentPosition.Position.Line;
            int startColumn = TextUtilities.PositionOfPrevDelimeter(
                                scriptFile.Contents,    
                                textDocumentPosition.Position.Line,
                                textDocumentPosition.Position.Character);
            int endColumn = textDocumentPosition.Position.Character;          

            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);
            if (scriptParseInfo != null && scriptParseInfo.ParseResult != null)
            {
                if (Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
                {
                    try
                    {
                        QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                            key: scriptParseInfo.ConnectionKey,
                            bindingTimeout: LanguageService.HoverTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {                          
                                // get the current quick info text
                                Babel.CodeObjectQuickInfo quickInfo = Resolver.GetQuickInfo(
                                    scriptParseInfo.ParseResult, 
                                    startLine + 1, 
                                    endColumn + 1, 
                                    bindingContext.MetadataDisplayInfoProvider);
                                
                                // convert from the parser format to the VS Code wire format
                                return AutoCompleteHelper.ConvertQuickInfoToHover(
                                        quickInfo, 
                                        startLine,
                                        startColumn, 
                                        endColumn);
                            });
                                        
                        queueItem.ItemProcessed.WaitOne();  
                        return queueItem.GetResultAsT<Hover>();     
                    }
                    finally
                    {
                        Monitor.Exit(scriptParseInfo.BuildingMetadataLock);
                    }               
                }
            }

            // return null if there isn't a tooltip for the current location
            return null;
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
            // initialize some state to parse and bind the current script file
            this.currentCompletionParseInfo = null;
            CompletionItem[] resultCompletionItems = null;
            string filePath = textDocumentPosition.TextDocument.Uri;
            int startLine = textDocumentPosition.Position.Line;
            int parserLine = textDocumentPosition.Position.Line + 1;
            int startColumn = TextUtilities.PositionOfPrevDelimeter(
                                scriptFile.Contents,    
                                textDocumentPosition.Position.Line,
                                textDocumentPosition.Position.Character);
            int endColumn = TextUtilities.PositionOfNextDelimeter(
                                scriptFile.Contents,    
                                textDocumentPosition.Position.Line,
                                textDocumentPosition.Position.Character);
            int parserColumn = textDocumentPosition.Position.Character + 1;
            bool useLowerCaseSuggestions = this.CurrentSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value;

            // get the current script parse info object
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);
            if (scriptParseInfo == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(
                    startLine, 
                    startColumn, 
                    endColumn, 
                    useLowerCaseSuggestions);
            }

            // reparse and bind the SQL statement if needed
            if (RequiresReparse(scriptParseInfo, scriptFile))
            {       
                ParseAndBind(scriptFile, connInfo);
            }

            // if the parse failed then return the default list
            if (scriptParseInfo.ParseResult == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(
                    startLine, 
                    startColumn, 
                    endColumn, 
                    useLowerCaseSuggestions);
            }
            
            // need to adjust line & column for base-1 parser indices
            Token token = GetToken(scriptParseInfo, parserLine, parserColumn);
            string tokenText = token != null ? token.Text : null;

            // check if the file is connected and the file lock is available
            if (scriptParseInfo.IsConnected && Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
            {         
                try
                {    
                    // queue the completion task with the binding queue    
                    QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                        key: scriptParseInfo.ConnectionKey,
                        bindingTimeout: LanguageService.BindingTimeout,
                        bindOperation: (bindingContext, cancelToken) =>
                        {
                            // get the completion list from SQL Parser
                            scriptParseInfo.CurrentSuggestions = Resolver.FindCompletions(
                                scriptParseInfo.ParseResult, 
                                parserLine, 
                                parserColumn, 
                                bindingContext.MetadataDisplayInfoProvider); 

                            // cache the current script parse info object to resolve completions later
                            this.currentCompletionParseInfo = scriptParseInfo;
                            
                            // convert the suggestion list to the VS Code format
                            return AutoCompleteHelper.ConvertDeclarationsToCompletionItems(
                                scriptParseInfo.CurrentSuggestions, 
                                startLine, 
                                startColumn, 
                                endColumn);
                        },
                        timeoutOperation: (bindingContext) =>
                        {
                            // return the default list if the connected bind fails
                            return AutoCompleteHelper.GetDefaultCompletionItems(
                                startLine, 
                                startColumn, 
                                endColumn, 
                                useLowerCaseSuggestions,
                                tokenText);
                        });

                    // wait for the queue item
                    queueItem.ItemProcessed.WaitOne();

                    var completionItems = queueItem.GetResultAsT<CompletionItem[]>();
                    if (completionItems != null && completionItems.Length > 0)
                    {
                        resultCompletionItems = completionItems;
                    }
                    else if (!ShouldShowCompletionList(token))
                    {
                        resultCompletionItems = AutoCompleteHelper.EmptyCompletionList;
                    }
                }
                finally
                {
                    Monitor.Exit(scriptParseInfo.BuildingMetadataLock);
                }
            }
            
            // if there are no completions then provide the default list
            if (resultCompletionItems == null)
            {
                resultCompletionItems = AutoCompleteHelper.GetDefaultCompletionItems(
                    startLine, 
                    startColumn, 
                    endColumn, 
                    useLowerCaseSuggestions,
                    tokenText);
            }

            return resultCompletionItems;
        }

        private static Token GetToken(ScriptParseInfo scriptParseInfo, int startLine, int startColumn)
        {
            if (scriptParseInfo != null && scriptParseInfo.ParseResult != null && scriptParseInfo.ParseResult.Script != null && scriptParseInfo.ParseResult.Script.Tokens != null)
            {
                var tokenIndex = scriptParseInfo.ParseResult.Script.TokenManager.FindToken(startLine, startColumn);
                if (tokenIndex >= 0)
                {
                    // return the current token
                    int currentIndex = 0;
                    foreach (var token in scriptParseInfo.ParseResult.Script.Tokens)
                    {
                        if (currentIndex == tokenIndex)
                        {
                            return token;
                        }
                        ++currentIndex;
                    }
                }
            }
            return null;
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

        #endregion

        #region Diagnostic Provider methods

        /// <summary>
        /// Gets a list of semantic diagnostic marks for the provided script file
        /// </summary>
        /// <param name="scriptFile"></param>
        internal ScriptFileMarker[] GetSemanticMarkers(ScriptFile scriptFile)
        {
            ConnectionInfo connInfo;
            ConnectionService.Instance.TryFindConnection(
                scriptFile.ClientFilePath, 
                out connInfo);
    
            var parseResult = ParseAndBind(scriptFile, connInfo);

            // build a list of SQL script file markers from the errors
            List<ScriptFileMarker> markers = new List<ScriptFileMarker>();
            if (parseResult != null && parseResult.Errors != null)
            {
                foreach (var error in parseResult.Errors)
                {
                    markers.Add(new ScriptFileMarker()
                    {
                        Message = error.Message,
                        Level = ScriptFileMarkerLevel.Error,
                        ScriptRegion = new ScriptRegion()
                        {
                            File = scriptFile.FilePath,
                            StartLineNumber = error.Start.LineNumber,
                            StartColumnNumber = error.Start.ColumnNumber,
                            StartOffset = 0,
                            EndLineNumber = error.End.LineNumber,
                            EndColumnNumber = error.End.ColumnNumber,
                            EndOffset = 0
                        }
                    });
                }
            }

            return markers.ToArray();
        }

        /// <summary>
        /// Runs script diagnostics on changed files
        /// </summary>
        /// <param name="filesToAnalyze"></param>
        /// <param name="eventContext"></param>
        private Task RunScriptDiagnostics(ScriptFile[] filesToAnalyze, EventContext eventContext)
        {
            if (!CurrentSettings.IsDiagnositicsEnabled)
            {
                // If the user has disabled script analysis, skip it entirely
                return Task.FromResult(true);
            }

            // If there's an existing task, attempt to cancel it
            try
            {
                if (ExistingRequestCancellation != null)
                {
                    // Try to cancel the request
                    ExistingRequestCancellation.Cancel();

                    // If cancellation didn't throw an exception,
                    // clean up the existing token
                    ExistingRequestCancellation.Dispose();
                    ExistingRequestCancellation = null;
                }
            }
            catch (Exception e)
            {
                Logger.Write(
                    LogLevel.Error,
                    string.Format(
                        "Exception while cancelling analysis task:\n\n{0}",
                        e.ToString()));

                TaskCompletionSource<bool> cancelTask = new TaskCompletionSource<bool>();
                cancelTask.SetCanceled();
                return cancelTask.Task;
            }

            // Create a fresh cancellation token and then start the task.
            // We create this on a different TaskScheduler so that we
            // don't block the main message loop thread.
            ExistingRequestCancellation = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                    DelayThenInvokeDiagnostics(
                        LanguageService.DiagnosticParseDelay,
                        filesToAnalyze,
                        eventContext,
                        ExistingRequestCancellation.Token),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Actually run the script diagnostics after waiting for some small delay
        /// </summary>
        /// <param name="delayMilliseconds"></param>
        /// <param name="filesToAnalyze"></param>
        /// <param name="eventContext"></param>
        /// <param name="cancellationToken"></param>
        private async Task DelayThenInvokeDiagnostics(
            int delayMilliseconds,
            ScriptFile[] filesToAnalyze,
            EventContext eventContext,
            CancellationToken cancellationToken)
        {
            // First of all, wait for the desired delay period before
            // analyzing the provided list of files
            try
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // If the task is cancelled, exit directly
                return;
            }

            // If we've made it past the delay period then we don't care
            // about the cancellation token anymore.  This could happen
            // when the user stops typing for long enough that the delay
            // period ends but then starts typing while analysis is going
            // on.  It makes sense to send back the results from the first
            // delay period while the second one is ticking away.

            // Get the requested files
            foreach (ScriptFile scriptFile in filesToAnalyze)
            {
                if (IsPreviewWindow(scriptFile))
                {
                    continue;
                }

                Logger.Write(LogLevel.Verbose, "Analyzing script file: " + scriptFile.FilePath);
                ScriptFileMarker[] semanticMarkers = GetSemanticMarkers(scriptFile);
                Logger.Write(LogLevel.Verbose, "Analysis complete.");

                await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, semanticMarkers, eventContext);
            }
        }

        #endregion

        /// <summary>
        /// Adds a new or updates an existing script parse info instance in local cache
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="scriptInfo"></param>
        internal void AddOrUpdateScriptParseInfo(string uri, ScriptParseInfo scriptInfo)
        {
            lock (this.parseMapLock)
            {
                if (this.ScriptParseInfoMap.ContainsKey(uri))
                {
                    this.ScriptParseInfoMap[uri] = scriptInfo;
                }
                else
                {
                    this.ScriptParseInfoMap.Add(uri, scriptInfo);
                }

            }
        }

        /// <summary>
        /// Gets a script parse info object for a file from the local cache
        /// Internal for testing purposes only
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="createIfNotExists">Creates a new instance if one doesn't exist</param>        
        internal ScriptParseInfo GetScriptParseInfo(string uri, bool createIfNotExists = false)
        {
            lock (this.parseMapLock)
            {
                if (this.ScriptParseInfoMap.ContainsKey(uri))
                {
                    return this.ScriptParseInfoMap[uri];
                }
                else if (createIfNotExists)
                {
                    // create a new script parse info object and initialize with the current settings
                    ScriptParseInfo scriptInfo = new ScriptParseInfo();
                    this.ScriptParseInfoMap.Add(uri, scriptInfo);
                    return scriptInfo;
                }
                else
                {
                    return null;
                }
            }
        }

        private bool RemoveScriptParseInfo(string uri)
        {
            lock (this.parseMapLock)
            {
                if (this.ScriptParseInfoMap.ContainsKey(uri))
                {                                   
                    return this.ScriptParseInfoMap.Remove(uri);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns a flag indicating if the ScriptFile refers to the output window.
        /// </summary>
        /// <param name="scriptFile"></param>
        private bool IsPreviewWindow(ScriptFile scriptFile)
        {
            if (scriptFile != null && !string.IsNullOrWhiteSpace(scriptFile.ClientFilePath))
            {
                return scriptFile.ClientFilePath.StartsWith("tsqloutput:");
            }
            else
            {
                return false;
            }
        }
    }
}
