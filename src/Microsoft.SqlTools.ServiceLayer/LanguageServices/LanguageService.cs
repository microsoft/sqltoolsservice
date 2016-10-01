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
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
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
        internal const string DefaultBatchSeperator = "GO";

        internal const int DiagnosticParseDelay = 750;

        internal const int FindCompletionsTimeout = 3000;

        internal const int FindCompletionStartTimeout = 50;

        internal const int OnConnectionWaitTimeout = 300000;

        private object parseMapLock = new object();

        private ScriptParseInfo currentCompletionParseInfo;

        private ConnectionService connectionService = null;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
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

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal LanguageService()
        {
        }

        #endregion

        #region Properties

        private static CancellationTokenSource ExistingRequestCancellation { get; set; }

        internal SqlToolsSettings CurrentSettings
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings; }
        }

        internal Workspace.Workspace CurrentWorkspace
        {
            get { return WorkspaceService<SqlToolsSettings>.Instance.Workspace; }
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
            serviceHost.SetRequestHandler(DefinitionRequest.Type, HandleDefinitionRequest);
            serviceHost.SetRequestHandler(ReferencesRequest.Type, HandleReferencesRequest);
            serviceHost.SetRequestHandler(CompletionResolveRequest.Type, HandleCompletionResolveRequest);
            serviceHost.SetRequestHandler(SignatureHelpRequest.Type, HandleSignatureHelpRequest);
            serviceHost.SetRequestHandler(DocumentHighlightRequest.Type, HandleDocumentHighlightRequest);
            serviceHost.SetRequestHandler(HoverRequest.Type, HandleHoverRequest);
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);

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
        private static async Task HandleCompletionRequest(
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

        private static async Task HandleDefinitionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Location[]> requestContext)
        {
            await Task.FromResult(true);
        }

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
            bool oldEnableIntelliSense = oldSettings.SqlTools.EnableIntellisense;
            bool? oldEnableDiagnostics = oldSettings.SqlTools.IntelliSense.EnableDiagnostics;

            // update the current settings to reflect any changes
            CurrentSettings.Update(newSettings);

            // update the script parse info objects if the settings have changed
            foreach (var scriptInfo in this.ScriptParseInfoMap.Values)
            {
                scriptInfo.OnSettingsChanged(newSettings);
            }

            // if script analysis settings have changed we need to clear the current diagnostic markers
            if (oldEnableIntelliSense != newSettings.SqlTools.EnableIntellisense
                || oldEnableDiagnostics != newSettings.SqlTools.IntelliSense.EnableDiagnostics)
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

            if (parseInfo.BuildingMetadataEvent.WaitOne(LanguageService.FindCompletionsTimeout))
            {
                try
                {
                    parseInfo.BuildingMetadataEvent.Reset();

                    // parse current SQL file contents to retrieve a list of errors
                    ParseResult parseResult = Parser.IncrementalParse(
                        scriptFile.Contents,
                        parseInfo.ParseResult,
                        parseInfo.ParseOptions);

                    parseInfo.ParseResult = parseResult;

                    if (connInfo != null && parseInfo.IsConnected)
                    {
                        try
                        {
                            List<ParseResult> parseResults = new List<ParseResult>();
                            parseResults.Add(parseResult);
                            parseInfo.Binder.Bind(
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
                    parseInfo.BuildingMetadataEvent.Set();
                }
            }
    
            return parseInfo.ParseResult;
        }

        /// <summary>
        /// Update the autocomplete metadata provider when the user connects to a database
        /// </summary>
        /// <param name="info"></param>
        public async Task UpdateLanguageServiceOnConnection(ConnectionInfo info)
        {
            await Task.Run( () => 
            {
                ScriptParseInfo scriptInfo = GetScriptParseInfo(info.OwnerUri, createIfNotExists: true);
                if (scriptInfo.BuildingMetadataEvent.WaitOne(LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {
                        scriptInfo.BuildingMetadataEvent.Reset();

                        ReliableSqlConnection sqlConn = info.SqlConnection as ReliableSqlConnection;
                        if (sqlConn != null)
                        {
                            ServerConnection serverConn = new ServerConnection(sqlConn.GetUnderlyingConnection());                            
                            scriptInfo.MetadataProvider = SmoMetadataProvider.CreateConnectedProvider(serverConn);
                            scriptInfo.Binder = BinderProvider.CreateBinder(scriptInfo.MetadataProvider);                           
                            scriptInfo.ServerConnection = serverConn;
                            scriptInfo.IsConnected = true;
                        }
                        
                    }
                    catch (Exception)
                    {
                        scriptInfo.IsConnected = false;
                    }
                    finally
                    {
                        // Set Metadata Build event to Signal state.
                        // (Tell Language Service that I am ready with Metadata Provider Object)
                        scriptInfo.BuildingMetadataEvent.Set();
                    }
                }

                // populate SMO metadata provider with most common info
                AutoCompleteHelper.PrepopulateCommonMetadata(info, scriptInfo);
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
            try
            {
                var scriptParseInfo = LanguageService.Instance.currentCompletionParseInfo;
                if (scriptParseInfo != null && scriptParseInfo.CurrentSuggestions != null)
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
                }
            }
            catch (Exception ex)
            {
                // if any exceptions are raised looking up extended completion metadata 
                // then just return the original completion item
                Logger.Write(LogLevel.Error, "Exeception in ResolveCompletionItem " + ex.ToString());
            }

            return completionItem;
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
                if (scriptParseInfo.BuildingMetadataEvent.WaitOne(LanguageService.FindCompletionStartTimeout))
                {
                    scriptParseInfo.BuildingMetadataEvent.Reset();
                    try
                    {
                        // get the current quick info text
                        Babel.CodeObjectQuickInfo quickInfo = Resolver.GetQuickInfo(
                            scriptParseInfo.ParseResult, 
                            startLine + 1, 
                            endColumn + 1, 
                            scriptParseInfo.MetadataDisplayInfoProvider);

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
                                        Line = startLine,
                                        Character = startColumn
                                    },
                                    End = new Position
                                    {
                                        Line = startLine,
                                        Character = endColumn
                                    }
                                }
                            };
                        }
                    }
                    finally
                    {
                        scriptParseInfo.BuildingMetadataEvent.Set();
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
            string filePath = textDocumentPosition.TextDocument.Uri;
            int startLine = textDocumentPosition.Position.Line;
            int startColumn = TextUtilities.PositionOfPrevDelimeter(
                                scriptFile.Contents,    
                                textDocumentPosition.Position.Line,
                                textDocumentPosition.Position.Character);
            int endColumn = textDocumentPosition.Position.Character;
            bool useLowerCaseSuggestions = this.CurrentSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value;

            this.currentCompletionParseInfo = null;

            // Take a reference to the list at a point in time in case we update and replace the list

            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);
            if (connInfo == null || scriptParseInfo == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(startLine, startColumn, endColumn, useLowerCaseSuggestions);
            }

            // reparse and bind the SQL statement if needed
            if (RequiresReparse(scriptParseInfo, scriptFile))
            {       
                ParseAndBind(scriptFile, connInfo);
            }

            if (scriptParseInfo.ParseResult == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(startLine, startColumn, endColumn, useLowerCaseSuggestions);
            }

            if (scriptParseInfo.IsConnected 
                && scriptParseInfo.BuildingMetadataEvent.WaitOne(LanguageService.FindCompletionStartTimeout))
            {
                scriptParseInfo.BuildingMetadataEvent.Reset();
                Task<CompletionItem[]> findCompletionsTask = Task.Run(() => {
                    try
                    {
                        // get the completion list from SQL Parser
                        scriptParseInfo.CurrentSuggestions = Resolver.FindCompletions(
                            scriptParseInfo.ParseResult, 
                            textDocumentPosition.Position.Line + 1, 
                            textDocumentPosition.Position.Character + 1, 
                            scriptParseInfo.MetadataDisplayInfoProvider); 

                        // cache the current script parse info object to resolve completions later
                        this.currentCompletionParseInfo = scriptParseInfo;

                        // convert the suggestion list to the VS Code format
                        return AutoCompleteHelper.ConvertDeclarationsToCompletionItems(
                            scriptParseInfo.CurrentSuggestions, 
                            startLine, 
                            startColumn, 
                            endColumn);
                    }
                    finally
                    {
                        scriptParseInfo.BuildingMetadataEvent.Set();
                    }
                });

                findCompletionsTask.Wait(LanguageService.FindCompletionsTimeout);
                if (findCompletionsTask.IsCompleted 
                    && findCompletionsTask.Result != null
                    && findCompletionsTask.Result.Length > 0)
                {
                    return findCompletionsTask.Result;
                }
            }
            
            return AutoCompleteHelper.GetDefaultCompletionItems(startLine, startColumn, endColumn, useLowerCaseSuggestions);
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
                Logger.Write(LogLevel.Verbose, "Analyzing script file: " + scriptFile.FilePath);
                ScriptFileMarker[] semanticMarkers = GetSemanticMarkers(scriptFile);
                Logger.Write(LogLevel.Verbose, "Analysis complete.");

                await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, semanticMarkers, eventContext);
            }
        }

        #endregion

        private void AddOrUpdateScriptParseInfo(string uri, ScriptParseInfo scriptInfo)
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

        private ScriptParseInfo GetScriptParseInfo(string uri, bool createIfNotExists = false)
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
                    scriptInfo.OnSettingsChanged(this.CurrentSettings);
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
                    var scriptInfo = this.ScriptParseInfoMap[uri];
                    scriptInfo.ServerConnection.Disconnect();
                    scriptInfo.ServerConnection = null;
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
