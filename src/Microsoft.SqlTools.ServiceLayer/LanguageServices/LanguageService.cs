//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
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
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that requires knowledge of
    /// the language to perform, such as definitions, intellisense, etc.
    /// </summary>
    public class LanguageService: IDisposable
    {
        #region Singleton Instance Implementation

        private static readonly Lazy<LanguageService> instance = new Lazy<LanguageService>(() => new LanguageService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static LanguageService Instance
        {
            get { return instance.Value; }
        }

        #endregion

        #region Private / internal instance fields and constructor
        private const int PrepopulateBindTimeout = 60000;

        public const string SQL_LANG = "SQL";
        private const int OneSecond = 1000;

        internal const string DefaultBatchSeperator = "GO";

        internal const int DiagnosticParseDelay = 750;

        internal const int HoverTimeout = 500;

        internal const int BindingTimeout = 500;

        internal const int OnConnectionWaitTimeout = 300 * OneSecond;

        internal const int PeekDefinitionTimeout = 10 * OneSecond;

        private ConnectionService connectionService = null;

        private WorkspaceService<SqlToolsSettings> workspaceServiceInstance;

        private ServiceHost serviceHostInstance;

        private object parseMapLock = new object();

        private ScriptParseInfo currentCompletionParseInfo;

        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue();

        private ParseOptions defaultParseOptions = new ParseOptions(
            batchSeparator: LanguageService.DefaultBatchSeperator,
            isQuotedIdentifierSet: true,
            compatibilityLevel: DatabaseCompatibilityLevel.Current,
            transactSqlVersion: TransactSqlVersion.Current);

        private ConcurrentDictionary<string, bool> nonMssqlUriMap = new ConcurrentDictionary<string, bool>();

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
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                    connectionService.RegisterConnectedQueue("LanguageService", bindingQueue);
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        private CancellationTokenSource existingRequestCancellation;

        /// <summary>
        /// Gets or sets the current workspace service instance
        /// Setter for internal testing purposes only
        /// </summary>
        internal WorkspaceService<SqlToolsSettings> WorkspaceServiceInstance
        {
            get
            {
                if (workspaceServiceInstance == null)
                {
                    workspaceServiceInstance =  WorkspaceService<SqlToolsSettings>.Instance;
                }
                return workspaceServiceInstance;
            }
            set
            {
                workspaceServiceInstance = value;
            }
        }

        internal ServiceHost ServiceHostInstance
        {
            get
            {
                if (this.serviceHostInstance == null)
                {
                    this.serviceHostInstance = ServiceHost.Instance;
                }
                return this.serviceHostInstance;
            }
            set
            {
                this.serviceHostInstance = value;
            }
        }

        /// <summary>
        /// Gets the current settings
        /// </summary>
        internal SqlToolsSettings CurrentWorkspaceSettings
        {
            get { return WorkspaceServiceInstance.CurrentSettings; }
        }

        /// <summary>
        /// Gets the current workspace instance
        /// </summary>
        internal Workspace.Workspace CurrentWorkspace
        {
            get { return WorkspaceServiceInstance.Workspace; }
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
            // serviceHost.SetRequestHandler(DocumentHighlightRequest.Type, HandleDocumentHighlightRequest);

            serviceHost.SetRequestHandler(SignatureHelpRequest.Type, HandleSignatureHelpRequest);
            serviceHost.SetRequestHandler(CompletionResolveRequest.Type, HandleCompletionResolveRequest);
            serviceHost.SetRequestHandler(HoverRequest.Type, HandleHoverRequest);
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);
            serviceHost.SetRequestHandler(DefinitionRequest.Type, HandleDefinitionRequest);
            serviceHost.SetEventHandler(RebuildIntelliSenseNotification.Type, HandleRebuildIntelliSenseNotification);
            serviceHost.SetEventHandler(LanguageFlavorChangeNotification.Type, HandleDidChangeLanguageFlavorNotification);

            // Register a no-op shutdown task for validation of the shutdown logic
            serviceHost.RegisterShutdownTask(async (shutdownParams, shutdownRequestContext) =>
            {
                Logger.Write(LogLevel.Verbose, "Shutting down language service");
                DeletePeekDefinitionScripts();
                this.Dispose();
                await Task.FromResult(0);
            });

            ServiceHostInstance = serviceHost;

            // Register the configuration update handler
            WorkspaceServiceInstance.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);

            // Register the file change update handler
            WorkspaceServiceInstance.RegisterTextDocChangeCallback(HandleDidChangeTextDocumentNotification);

            // Register the file open update handler
            WorkspaceServiceInstance.RegisterTextDocOpenCallback(HandleDidOpenTextDocumentNotification);

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
        internal async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {            
            try
            {            
                // check if Intellisense suggestions are enabled
                if (ShouldSkipIntellisense(textDocumentPosition.TextDocument.Uri))
                {
                    await requestContext.SendResult(null);
                }
                else
                {
                    // get the current list of completion items and return to client
                    var scriptFile = CurrentWorkspace.GetFile(
                        textDocumentPosition.TextDocument.Uri);
                    if (scriptFile == null)
                    {
                        await requestContext.SendResult(null);
                        return;
                    }

                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        scriptFile.ClientFilePath,
                        out connInfo);

                    var completionItems = GetCompletionItems(
                        textDocumentPosition, scriptFile, connInfo);

                    await requestContext.SendResult(completionItems);       
                }
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handle the resolve completion request event to provide additional
        /// autocomplete metadata to the currently select completion item
        /// </summary>
        /// <param name="completionItem"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal async Task HandleCompletionResolveRequest(
            CompletionItem completionItem,
            RequestContext<CompletionItem> requestContext)
        {
            try
            {
                // check if Intellisense suggestions are enabled
                // Note: Do not know file, so no need to check for MSSQL flavor
                if (!CurrentWorkspaceSettings.IsSuggestionsEnabled)
                {
                    await requestContext.SendResult(completionItem);
                }
                else
                {
                    completionItem = ResolveCompletionItem(completionItem);
                    await requestContext.SendResult(completionItem);
                }
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal async Task HandleDefinitionRequest(TextDocumentPosition textDocumentPosition, RequestContext<Location[]> requestContext)
        {
            try 
            {
                DocumentStatusHelper.SendStatusChange(requestContext, textDocumentPosition, DocumentStatusHelper.DefinitionRequested);

                if (!ShouldSkipIntellisense(textDocumentPosition.TextDocument.Uri))
                {
                    // Retrieve document and connection
                    ConnectionInfo connInfo;
                    var scriptFile = CurrentWorkspace.GetFile(textDocumentPosition.TextDocument.Uri);
                    bool isConnected = false;
                    bool succeeded = false;
                    DefinitionResult definitionResult = null;
                    if (scriptFile != null)
                    {
                        isConnected = ConnectionServiceInstance.TryFindConnection(scriptFile.ClientFilePath, out connInfo);
                        definitionResult = GetDefinition(textDocumentPosition, scriptFile, connInfo);
                    }
                    
                    if (definitionResult != null)
                    {
                        if (definitionResult.IsErrorResult)
                        {
                            await requestContext.SendError(definitionResult.Message);
                        }
                        else
                        {
                            await requestContext.SendResult(definitionResult.Locations);
                            succeeded = true;
                        }
                    }
                    else
                    {
                        await requestContext.SendResult(Array.Empty<Location>());
                    }

                    DocumentStatusHelper.SendTelemetryEvent(requestContext, CreatePeekTelemetryProps(succeeded, isConnected));
                }
                else
                {
                    // Send an empty result so that processing does not hang when peek def service called from non-mssql clients
                    await requestContext.SendResult(Array.Empty<Location>());
                }

                DocumentStatusHelper.SendStatusChange(requestContext, textDocumentPosition, DocumentStatusHelper.DefinitionRequestCompleted);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private static TelemetryProperties CreatePeekTelemetryProps(bool succeeded, bool connected)
        {
            return new TelemetryProperties
            {
                Properties = new Dictionary<string, string>
                {
                    { TelemetryPropertyNames.Succeeded, succeeded.ToOneOrZeroString() },
                    { TelemetryPropertyNames.Connected, connected.ToOneOrZeroString() }
                },
                EventName = TelemetryEventNames.PeekDefinitionRequested
            };
        }

// turn off this code until needed (10/28/2016)
#if false
        private async Task HandleReferencesRequest(
            ReferencesParams referencesParams,
            RequestContext<Location[]> requestContext)
        {
            await requestContext.SendResult(null);
        }

        private async Task HandleDocumentHighlightRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<DocumentHighlight[]> requestContext)
        {
            await requestContext.SendResult(null);
        }
#endif

        internal async Task HandleSignatureHelpRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<SignatureHelp> requestContext)
        {
            try
            {
                // check if Intellisense suggestions are enabled
                if (ShouldSkipNonMssqlFile(textDocumentPosition))
                {
                    await requestContext.SendResult(null);
                }
                else
                {
                    ScriptFile scriptFile = CurrentWorkspace.GetFile(
                        textDocumentPosition.TextDocument.Uri);
                    SignatureHelp help = null;
                    if (scriptFile != null)
                    {
                        help = GetSignatureHelp(textDocumentPosition, scriptFile);
                    }
                    if (help != null)
                    {
                        await requestContext.SendResult(help);
                    }
                    else
                    {
                        await requestContext.SendResult(new SignatureHelp());
                    }
                }
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {
            try
            {
                // check if Quick Info hover tooltips are enabled
                if (CurrentWorkspaceSettings.IsQuickInfoEnabled
                    && !ShouldSkipNonMssqlFile(textDocumentPosition))
                {
                    var scriptFile = CurrentWorkspace.GetFile(
                        textDocumentPosition.TextDocument.Uri);

                    Hover hover = null;
                    if (scriptFile != null)
                    {
                        hover = GetHoverItem(textDocumentPosition, scriptFile);
                    }
                    if (hover != null)
                    {
                        await requestContext.SendResult(hover);
                    }
                }

                await requestContext.SendResult(new Hover());
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
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
            try
            {
                // if not in the preview window and diagnostics are enabled then run diagnostics
                if (!IsPreviewWindow(scriptFile)
                    && CurrentWorkspaceSettings.IsDiagnosticsEnabled)
                {
                    await RunScriptDiagnostics(
                        new ScriptFile[] { scriptFile },
                        eventContext);
                }

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                // TODO: need mechanism return errors from event handlers
            }
        }

        /// <summary>
        /// Handles text document change events
        /// </summary>
        /// <param name="textChangeParams"></param>
        /// <param name="eventContext"></param>
        public async Task HandleDidChangeTextDocumentNotification(ScriptFile[] changedFiles, EventContext eventContext)
        {
            try
            {
                if (CurrentWorkspaceSettings.IsDiagnosticsEnabled)
                {
                    // Only process files that are MSSQL flavor
                    await this.RunScriptDiagnostics(
                        changedFiles.ToArray(),
                        eventContext);
                }

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                // TODO: need mechanism return errors from event handlers
            }
        }

        /// <summary>
        /// Handle the rebuild IntelliSense cache notification
        /// </summary>
        public async Task HandleRebuildIntelliSenseNotification(
            RebuildIntelliSenseParams rebuildParams,
            EventContext eventContext)
        {
            try
            {
                Logger.Write(LogLevel.Verbose, "HandleRebuildIntelliSenseNotification");

                // Skip closing this file if the file doesn't exist
                var scriptFile = this.CurrentWorkspace.GetFile(rebuildParams.OwnerUri);
                if (scriptFile == null)
                {
                    return;
                }

                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    scriptFile.ClientFilePath,
                    out connInfo);

                // check that there is an active connection for the current editor
                if (connInfo != null)
                {
                    await Task.Run(() =>
                    {
                        ScriptParseInfo scriptInfo = GetScriptParseInfo(connInfo.OwnerUri, createIfNotExists: false);
                        if (scriptInfo != null && scriptInfo.IsConnected && 
                            Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                        {
                            try
                            {
                                this.BindingQueue.AddConnectionContext(connInfo, featureName: "LanguageService", overwrite: true);
                            }
                            catch (Exception ex)
                            {
                                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                            }
                            finally
                            {
                                // Set Metadata Build event to Signal state.
                                Monitor.Exit(scriptInfo.BuildingMetadataLock);
                            }
                        }

                        // if not in the preview window and diagnostics are enabled then run diagnostics
                        if (!IsPreviewWindow(scriptFile)
                            && CurrentWorkspaceSettings.IsDiagnosticsEnabled)
                        {
                            RunScriptDiagnostics(
                                new ScriptFile[] { scriptFile },
                                eventContext);
                        }

                        // Send a notification to signal that autocomplete is ready
                        ServiceHostInstance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = connInfo.OwnerUri});
                    });
                }
                else
                {
                    // Send a notification to signal that autocomplete is ready
                    await ServiceHostInstance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = rebuildParams.OwnerUri});
                }
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                await ServiceHostInstance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = rebuildParams.OwnerUri});
            }
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
            try
            {
                bool oldEnableIntelliSense = oldSettings.SqlTools.IntelliSense.EnableIntellisense;
                bool? oldEnableDiagnostics = oldSettings.SqlTools.IntelliSense.EnableErrorChecking;

                // update the current settings to reflect any changes
                CurrentWorkspaceSettings.Update(newSettings);

                // if script analysis settings have changed we need to clear the current diagnostic markers
                if (oldEnableIntelliSense != newSettings.SqlTools.IntelliSense.EnableIntellisense
                    || oldEnableDiagnostics != newSettings.SqlTools.IntelliSense.EnableErrorChecking)
                {
                    // if the user just turned off diagnostics then send an event to clear the error markers
                    if (!newSettings.IsDiagnosticsEnabled)
                    {
                        ScriptFileMarker[] emptyAnalysisDiagnostics = new ScriptFileMarker[0];

                        foreach (var scriptFile in CurrentWorkspace.GetOpenedFiles())
                        {
                            await DiagnosticsHelper.ClearScriptDiagnostics(scriptFile.ClientFilePath, eventContext);
                        }
                    }
                    // otherwise rerun diagnostic analysis on all opened SQL files
                    else
                    {
                        await this.RunScriptDiagnostics(CurrentWorkspace.GetOpenedFiles(), eventContext);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                // TODO: need mechanism return errors from event handlers
            }
        }

        /// <summary>
        /// Handles language flavor changes by disabling intellisense on a file if it does not match the specific
        /// "MSSQL" language flavor returned by our service
        /// </summary>
        /// <param name="info"></param>
        public async Task HandleDidChangeLanguageFlavorNotification(
            LanguageFlavorChangeParams changeParams,
            EventContext eventContext) 
        {
            try
            {
                Validate.IsNotNull(nameof(changeParams), changeParams);
                Validate.IsNotNull(nameof(changeParams), changeParams.Uri);
                bool shouldBlock = false;
                if (SQL_LANG.Equals(changeParams.Language, StringComparison.OrdinalIgnoreCase)) {
                    shouldBlock = !ServiceHost.ProviderName.Equals(changeParams.Flavor, StringComparison.OrdinalIgnoreCase);
                }

                if (shouldBlock) {
                    this.nonMssqlUriMap.AddOrUpdate(changeParams.Uri, true, (k, oldValue) => true);
                    if (CurrentWorkspace.ContainsFile(changeParams.Uri))
                    {
                        await DiagnosticsHelper.ClearScriptDiagnostics(changeParams.Uri, eventContext);
                    }
                }
                else
                {
                    bool value;
                    this.nonMssqlUriMap.TryRemove(changeParams.Uri, out value);
                }
            }
            catch (Exception ex)
            {
                Logger.Write(LogLevel.Error, "Unknown error " + ex.ToString());
                // TODO: need mechanism return errors from event handlers
            }
        }

        #endregion


        #region "AutoComplete Provider methods"

        /// <summary>
        /// Remove a reference to an autocomplete cache from a URI. If
        /// it is the last URI connected to a particular connection,
        /// then remove the cache.
        /// </summary>
        public async Task RemoveAutoCompleteCacheUriReference(IConnectionSummary summary, string ownerUri)
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
                            // parse on separate thread so stack size can be increased
                            var parseThread = new Thread(() =>
                            {
                                 // parse current SQL file contents to retrieve a list of errors
                                ParseResult parseResult = Parser.IncrementalParse(
                                    scriptFile.Contents,
                                    parseInfo.ParseResult,
                                    this.DefaultParseOptions);

                                parseInfo.ParseResult = parseResult;
                            }, ConnectedBindingQueue.QueueThreadStackSize);
                            parseThread.Start();
                            parseThread.Join();                        
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
                                    if (bindingContext.IsConnected && bindingContext.Binder != null)
                                    {
                                        bindingContext.Binder.Bind(
                                            parseResults,
                                            connInfo.ConnectionDetails.DatabaseName,
                                            BindMode.Batch);
                                    }
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
                if (ConnectionService.IsDedicatedAdminConnection(info.ConnectionDetails))
                {
                    // Intellisense cannot be run on these connections as only 1 SqlConnection can be opened on them at a time
                    return;
                }
                ScriptParseInfo scriptInfo = GetScriptParseInfo(info.OwnerUri, createIfNotExists: true);
                if (Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {
                        scriptInfo.ConnectionKey = this.BindingQueue.AddConnectionContext(info, "languageService");
                        scriptInfo.IsConnected = this.BindingQueue.IsBindingContextConnected(scriptInfo.ConnectionKey);
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

                PrepopulateCommonMetadata(info, scriptInfo, this.BindingQueue);

                // Send a notification to signal that autocomplete is ready
                ServiceHostInstance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = info.OwnerUri});
            });
        }


        /// <summary>
        /// Preinitialize the parser and binder with common metadata.
        /// This should front load the long binding wait to the time the
        /// connection is established.  Once this is completed other binding
        /// requests should be faster.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="scriptInfo"></param>
        internal void PrepopulateCommonMetadata(
            ConnectionInfo info,
            ScriptParseInfo scriptInfo,
            ConnectedBindingQueue bindingQueue)
        {
            if (scriptInfo.IsConnected)
            {
                var scriptFile = CurrentWorkspace.GetFile(info.OwnerUri);
                if (scriptFile == null)
                {
                    return;
                }

                ParseAndBind(scriptFile, info);

                if (Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {
                        QueueItem queueItem = bindingQueue.QueueBindingOperation(
                            key: scriptInfo.ConnectionKey,
                            bindingTimeout: PrepopulateBindTimeout,
                            waitForLockTimeout: PrepopulateBindTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {
                                // parse a simple statement that returns common metadata
                                ParseResult parseResult = Parser.Parse(
                                    "select ",
                                    bindingContext.ParseOptions);
                                if (bindingContext.IsConnected && bindingContext.Binder != null)
                                {
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
                                }
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

        private bool ShouldSkipNonMssqlFile(TextDocumentPosition textDocPosition)
        {
            return ShouldSkipNonMssqlFile(textDocPosition.TextDocument.Uri);
        }

        private bool ShouldSkipNonMssqlFile(ScriptFile scriptFile)
        {
            return ShouldSkipNonMssqlFile(scriptFile.ClientFilePath);
        }

        /// <summary>
        /// Checks if a given URI is not an MSSQL file. Only files explicitly excluded by a language flavor change
        /// notification will be treated as skippable
        /// </summary>
        public virtual bool ShouldSkipNonMssqlFile(string uri)
        {
            bool isNonMssql = false;
            nonMssqlUriMap.TryGetValue(uri, out isNonMssql);
            return isNonMssql;
        }

        /// <summary>
        /// Determines whether intellisense should be skipped for a document.
        /// If IntelliSense is disabled or it's a non-MSSQL doc this will be skipped
        /// </summary>
        private bool ShouldSkipIntellisense(string uri)
        {
            return !CurrentWorkspaceSettings.IsSuggestionsEnabled
                || ShouldSkipNonMssqlFile(uri);
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
            var scriptParseInfo = currentCompletionParseInfo;
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
                        Logger.Write(LogLevel.Error, "Exception in ResolveCompletionItem " + ex.ToString());
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
        /// Queue a task to the binding queue
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptParseInfo"></param>
        /// <param name="connectionInfo"></param>
        /// <param name="scriptFile"></param>
        /// <param name="tokenText"></param>
        /// <returns> Returns the result of the task as a DefinitionResult </returns>
        private DefinitionResult QueueTask(TextDocumentPosition textDocumentPosition, ScriptParseInfo scriptParseInfo, 
                                            ConnectionInfo connInfo, ScriptFile scriptFile, string tokenText)
        {
            // Queue the task with the binding queue
            QueueItem queueItem = this.BindingQueue.QueueBindingOperation(
                key: scriptParseInfo.ConnectionKey,
                bindingTimeout: LanguageService.PeekDefinitionTimeout,
                bindOperation: (bindingContext, cancelToken) =>
                {
                    string schemaName = this.GetSchemaName(scriptParseInfo, textDocumentPosition.Position, scriptFile);
                    // Script object using SMO
                    Scripter scripter = new Scripter(bindingContext.ServerConnection, connInfo);
                    return scripter.GetScript(
                        scriptParseInfo.ParseResult, 
                        textDocumentPosition.Position, 
                        bindingContext.MetadataDisplayInfoProvider, 
                        tokenText, 
                        schemaName);
                },
                timeoutOperation: (bindingContext) =>
                {
                    // return error result
                    return new DefinitionResult
                    {
                        IsErrorResult = true,
                        Message = SR.PeekDefinitionTimedoutError,
                        Locations = null
                    };
                },
                errorHandler: ex =>
                {
                    // return error result
                    return new DefinitionResult
                    {
                        IsErrorResult = true,
                        Message = ex.Message,
                        Locations = null
                    };
                });
                           
            // wait for the queue item
            queueItem.ItemProcessed.WaitOne();
            var result = queueItem.GetResultAsT<DefinitionResult>();
            return result;
        }

        private DefinitionResult GetDefinitionFromTokenList(TextDocumentPosition textDocumentPosition, List<Token> tokenList,
                ScriptParseInfo scriptParseInfo, ScriptFile scriptFile, ConnectionInfo connInfo)
        {

            DefinitionResult lastResult = null;
            foreach (var token in tokenList)
            {

                // Strip "[" and "]"(if present) from the token text to enable matching with the suggestions.
                // The suggestion title does not contain any sql punctuation
                string tokenText = TextUtilities.RemoveSquareBracketSyntax(token.Text);
                textDocumentPosition.Position.Line = token.StartLocation.LineNumber;
                textDocumentPosition.Position.Character = token.StartLocation.ColumnNumber;
                if (Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
                {
                    try
                    {
                        var result = QueueTask(textDocumentPosition, scriptParseInfo, connInfo, scriptFile, tokenText);
                        lastResult = result;
                        if (!result.IsErrorResult)
                        {
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // if any exceptions are raised return error result with message
                        Logger.Write(LogLevel.Error, "Exception in GetDefinition " + ex.ToString());
                        return new DefinitionResult
                        {
                            IsErrorResult = true,
                            Message = SR.PeekDefinitionError(ex.Message),
                            Locations = null
                        };
                    }
                    finally
                    {
                        Monitor.Exit(scriptParseInfo.BuildingMetadataLock);
                    }
                }
                else
                {
                    Logger.Write(LogLevel.Error, "Timeout waiting to query metadata from server");
                }
            }
            return (lastResult != null) ? lastResult : null;
        }

        /// <summary>
        /// Get definition for a selected sql object using SMO Scripting
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptFile"></param>
        /// <param name="connInfo"></param>
        /// <returns> Location with the URI of the script file</returns>
        internal DefinitionResult GetDefinition(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ConnectionInfo connInfo)
        {
            // Parse sql
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);
            if (scriptParseInfo == null)
            {
                return null;
            }

            if (RequiresReparse(scriptParseInfo, scriptFile))
            {
                scriptParseInfo.ParseResult = ParseAndBind(scriptFile, connInfo);
            }

            // Get token from selected text
            Tuple<Stack<Token>, Queue<Token>> selectedToken = ScriptDocumentInfo.GetPeekDefinitionTokens(scriptParseInfo, 
                textDocumentPosition.Position.Line + 1, textDocumentPosition.Position.Character + 1);

            if (selectedToken == null)
            {
                return null;
            }

            if (scriptParseInfo.IsConnected)
            {
                //try children tokens first
                Stack<Token> childrenTokens = selectedToken.Item1;
                List<Token> tokenList = childrenTokens.ToList();
                DefinitionResult childrenResult = GetDefinitionFromTokenList(textDocumentPosition, tokenList, scriptParseInfo, scriptFile, connInfo);

                // if the children peak definition returned null then 
                // try the parents
                if (childrenResult == null || childrenResult.IsErrorResult)
                {
                    Queue<Token> parentTokens = selectedToken.Item2;
                    tokenList = parentTokens.ToList();
                    DefinitionResult parentResult = GetDefinitionFromTokenList(textDocumentPosition, tokenList, scriptParseInfo, scriptFile, connInfo);
                    return (parentResult == null) ? null : parentResult;
                }
                else
                {
                    return childrenResult;
                }    
            }
            else
            {
                // User is not connected.
                return new DefinitionResult
                {
                    IsErrorResult = true,
                    Message = SR.PeekDefinitionNotConnectedError,
                    Locations = null
                };
            }
        }
   
        /// <summary>
        /// Wrapper around find token method 
        /// </summary>
        /// <param name="scriptParseInfo"></param>
        /// <param name="startLine"></param>
        /// <param name="startColumn"></param>
        /// <returns> token index</returns>
        private int FindTokenWithCorrectOffset(ScriptParseInfo scriptParseInfo, int startLine, int startColumn)
        {
            var tokenIndex = scriptParseInfo.ParseResult.Script.TokenManager.FindToken(startLine, startColumn);
            var end = scriptParseInfo.ParseResult.Script.TokenManager.GetToken(tokenIndex).EndLocation;
            if (end.LineNumber == startLine && end.ColumnNumber == startColumn)
            {
                return tokenIndex + 1;
            }
            return tokenIndex;
        }

        /// <summary>
        /// Extract schema name for a token, if present
        /// </summary>
        /// <param name="scriptParseInfo"></param>
        /// <param name="position"></param>
        /// <param name="scriptFile"></param>
        /// <returns> schema name</returns>
        private string GetSchemaName(ScriptParseInfo scriptParseInfo, Position position, ScriptFile scriptFile)
        {
            // Offset index by 1 for sql parser
            int startLine = position.Line;
            int startColumn = position.Character;

            // Get schema name
            if (scriptParseInfo != null && scriptParseInfo.ParseResult != null && scriptParseInfo.ParseResult.Script != null && scriptParseInfo.ParseResult.Script.Tokens != null)
            {
                var tokenIndex = FindTokenWithCorrectOffset(scriptParseInfo, startLine, startColumn);
                var prevTokenIndex = scriptParseInfo.ParseResult.Script.TokenManager.GetPreviousSignificantTokenIndex(tokenIndex);
                var prevTokenText = scriptParseInfo.ParseResult.Script.TokenManager.GetText(prevTokenIndex);
                if (prevTokenText != null && prevTokenText.Equals("."))
                {
                    var schemaTokenIndex = scriptParseInfo.ParseResult.Script.TokenManager.GetPreviousSignificantTokenIndex(prevTokenIndex);
                    Token schemaToken = scriptParseInfo.ParseResult.Script.TokenManager.GetToken(schemaTokenIndex);
                    return TextUtilities.RemoveSquareBracketSyntax(schemaToken.Text);
                }
            }
            // if no schema name, returns null
            return null;
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
        /// Get function signature help for the current position
        /// </summary>
        internal SignatureHelp GetSignatureHelp(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile)
        {
            int startLine = textDocumentPosition.Position.Line;
            int startColumn = TextUtilities.PositionOfPrevDelimeter(
                                scriptFile.Contents,
                                textDocumentPosition.Position.Line,
                                textDocumentPosition.Position.Character);
            int endColumn = textDocumentPosition.Position.Character;

            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);

            if (scriptParseInfo == null)
            {
                // Cache not set up yet - skip and wait until later
                return null;
            }

            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                scriptFile.ClientFilePath,
                out connInfo);

            // reparse and bind the SQL statement if needed
            if (RequiresReparse(scriptParseInfo, scriptFile))
            {
                ParseAndBind(scriptFile, connInfo);
            }

            if (scriptParseInfo.ParseResult != null)
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
                                // get the list of possible current methods for signature help
                                var methods = Resolver.FindMethods(
                                    scriptParseInfo.ParseResult,
                                    startLine + 1,
                                    endColumn + 1,
                                    bindingContext.MetadataDisplayInfoProvider);

                                // get positional information on the current method
                                var methodLocations = Resolver.GetMethodNameAndParams(scriptParseInfo.ParseResult,
                                   startLine + 1,
                                   endColumn + 1,
                                   bindingContext.MetadataDisplayInfoProvider);

                                if (methodLocations != null)
                                {
                                    // convert from the parser format to the VS Code wire format
                                    return AutoCompleteHelper.ConvertMethodHelpTextListToSignatureHelp(methods,
                                        methodLocations,
                                        startLine + 1,
                                        endColumn + 1);
                                }
                                else
                                {
                                    return null;
                                }
                            });

                        queueItem.ItemProcessed.WaitOne();
                        return queueItem.GetResultAsT<SignatureHelp>();
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
            CompletionService completionService = new CompletionService(BindingQueue);
            bool useLowerCaseSuggestions = this.CurrentWorkspaceSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value;

            // get the current script parse info object
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(textDocumentPosition.TextDocument.Uri);

            if (scriptParseInfo == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(ScriptDocumentInfo.CreateDefaultDocumentInfo(textDocumentPosition, scriptFile), useLowerCaseSuggestions);
            }

            ScriptDocumentInfo scriptDocumentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);

            // reparse and bind the SQL statement if needed
            if (RequiresReparse(scriptParseInfo, scriptFile))
            {
                ParseAndBind(scriptFile, connInfo);
            }

            // if the parse failed then return the default list
            if (scriptParseInfo.ParseResult == null)
            {
                return AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, useLowerCaseSuggestions);
            }
            AutoCompletionResult result = completionService.CreateCompletions(connInfo, scriptDocumentInfo, useLowerCaseSuggestions);
            // cache the current script parse info object to resolve completions later
            this.currentCompletionParseInfo = scriptParseInfo;
            resultCompletionItems = result.CompletionItems;

            // if there are no completions then provide the default list
            if (resultCompletionItems == null)
            {
                resultCompletionItems = AutoCompleteHelper.GetDefaultCompletionItems(scriptDocumentInfo, useLowerCaseSuggestions);
            }

            return resultCompletionItems;
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
            ConnectionServiceInstance.TryFindConnection(
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
            if (!CurrentWorkspaceSettings.IsDiagnosticsEnabled)
            {
                // If the user has disabled script analysis, skip it entirely
                return Task.FromResult(true);
            }

            // If there's an existing task, attempt to cancel it
            try
            {
                if (existingRequestCancellation != null)
                {
                    // Try to cancel the request
                    existingRequestCancellation.Cancel();

                    // If cancellation didn't throw an exception,
                    // clean up the existing token
                    existingRequestCancellation.Dispose();
                    existingRequestCancellation = null;
                }
            }
            catch (Exception e)
            {
                Logger.Write(LogLevel.Error, string.Format("Exception while cancelling analysis task:\n\n{0}", e.ToString()));

                TaskCompletionSource<bool> cancelTask = new TaskCompletionSource<bool>();
                cancelTask.SetCanceled();
                return cancelTask.Task;
            }

            // Create a fresh cancellation token and then start the task.
            // We create this on a different TaskScheduler so that we
            // don't block the main message loop thread.
            existingRequestCancellation = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                    DelayThenInvokeDiagnostics(
                        LanguageService.DiagnosticParseDelay,
                        filesToAnalyze,
                        eventContext,
                        existingRequestCancellation.Token),
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
                else if (ShouldSkipNonMssqlFile(scriptFile.ClientFilePath))
                {
                    // Clear out any existing markers in case file type was changed
                    await DiagnosticsHelper.ClearScriptDiagnostics(scriptFile.ClientFilePath, eventContext);
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

        internal bool RemoveScriptParseInfo(string uri)
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
        internal bool IsPreviewWindow(ScriptFile scriptFile)
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

        internal void DeletePeekDefinitionScripts()
        {
            // Delete temp folder created to store peek definition scripts
            if (FileUtilities.SafeDirectoryExists(FileUtilities.PeekDefinitionTempFolder))
            {
                FileUtilities.SafeDirectoryDelete(FileUtilities.PeekDefinitionTempFolder, true);
            }
        }

        internal string ParseStatementAtPosition(string sql, int line, int column)
        {
            // adjust from 0-based to 1-based index
            int parserLine = line + 1;
            int parserColumn = column + 1;

            // parse current SQL file contents to retrieve a list of errors
            ParseResult parseResult = Parser.Parse(sql, this.DefaultParseOptions);
            if (parseResult != null && parseResult.Script != null && parseResult.Script.Batches != null)
            {
                foreach (var batch in parseResult.Script.Batches)
                {
                    if (batch.Statements == null)
                    {
                        continue;
                    }

                    // If there is a single statement on the line, track it so that we can return it regardless of where the user's cursor is
                    SqlStatement lineStatement = null;
                    bool? lineHasSingleStatement = null;

                    // check if the batch matches parameters
                    if (batch.StartLocation.LineNumber <= parserLine 
                        && batch.EndLocation.LineNumber >= parserLine)
                    {
                        foreach (var statement in batch.Statements)
                        {
                            // check if the statement matches parameters
                            if (statement.StartLocation.LineNumber <= parserLine 
                                && statement.EndLocation.LineNumber >= parserLine)
                            {
                                if (statement.EndLocation.LineNumber == parserLine && statement.EndLocation.ColumnNumber < parserColumn
                                    || statement.StartLocation.LineNumber == parserLine && statement.StartLocation.ColumnNumber > parserColumn)
                                {
                                    if (lineHasSingleStatement == null)
                                    {
                                        lineHasSingleStatement = true;
                                        lineStatement = statement;
                                    }
                                    else if (lineHasSingleStatement == true)
                                    {
                                        lineHasSingleStatement = false;
                                    }
                                    continue;
                                }
                                return statement.Sql;
                            }
                        }
                    }

                    if (lineHasSingleStatement == true)
                    {
                        return lineStatement.Sql;
                    }
                }
            }

            return string.Empty;
        }

        public void Dispose()
        {
            if (bindingQueue != null)
            {
                bindingQueue.Dispose();
            }
        }
    }
}