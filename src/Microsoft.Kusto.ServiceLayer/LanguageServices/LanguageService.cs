//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Completion;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.Kusto.ServiceLayer.Workspace;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Utility;
using Location = Microsoft.Kusto.ServiceLayer.Workspace.Contracts.Location;
using SqlToolsSettings = Microsoft.Kusto.ServiceLayer.SqlContext.SqlToolsSettings;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Main class for Language Service functionality including anything that requires knowledge of
    /// the language to perform, such as definitions, intellisense, etc.
    /// </summary>
    public class LanguageService: IDisposable
    {
        private static IDataSourceFactory _dataSourceFactory;

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

        #region Instance fields and constructor

        private const int OneSecond = 1000;

        private const int PrepopulateBindTimeout = 60000;

        internal const string DefaultBatchSeperator = "GO";

        internal const int DiagnosticParseDelay = 750;

        internal const int HoverTimeout = 500;

        internal const int BindingTimeout = 500;

        internal const int OnConnectionWaitTimeout = 300 * OneSecond;

        internal const int PeekDefinitionTimeout = 10 * OneSecond;

        // For testability only
        internal Task DelayedDiagnosticsTask = null;

        private ConnectionService connectionService = null;

        private WorkspaceService<SqlToolsSettings> workspaceServiceInstance;

        private ServiceHost serviceHostInstance;

        private object parseMapLock = new object();

        private ScriptParseInfo currentCompletionParseInfo;

        private IConnectedBindingQueue _bindingQueue;

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
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                    connectionService.RegisterConnectedQueue("LanguageService", _bindingQueue);
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the Language Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        /// <param name="dataSourceFactory"></param>
        /// <param name="connectedBindingQueue"></param>
        public void InitializeService(ServiceHost serviceHost, IDataSourceFactory dataSourceFactory, IConnectedBindingQueue connectedBindingQueue)
        {
            _dataSourceFactory = dataSourceFactory;
            _bindingQueue = connectedBindingQueue;
            // Register the requests that this service will handle

            //serviceHost.SetRequestHandler(SignatureHelpRequest.Type, HandleSignatureHelpRequest);     // Kusto api doesnt support this as of now. Implement it wherever applicable. Hover help is closest to signature help
            serviceHost.SetRequestHandler(CompletionResolveRequest.Type, HandleCompletionResolveRequest);
            serviceHost.SetRequestHandler(HoverRequest.Type, HandleHoverRequest);
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);
            serviceHost.SetRequestHandler(DefinitionRequest.Type, HandleDefinitionRequest);             // Parses "Go to definition" functionality
            serviceHost.SetRequestHandler(SyntaxParseRequest.Type, HandleSyntaxParseRequest);           // Parses syntax errors
            serviceHost.SetEventHandler(RebuildIntelliSenseNotification.Type, HandleRebuildIntelliSenseNotification);

            // Register a no-op shutdown task for validation of the shutdown logic
            serviceHost.RegisterShutdownTask(async (shutdownParams, shutdownRequestContext) =>
            {
                Logger.Write(TraceEventType.Verbose, "Shutting down language service");
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
            
        }

        #endregion

        #region Request Handlers

        /// <summary>
        /// T-SQL syntax parse request callback
        /// </summary>
        /// <param name="param"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal async Task HandleSyntaxParseRequest(SyntaxParseParams param, RequestContext<SyntaxParseResult> requestContext)
        {
            await Task.Run(async () =>
            {
                try
                {
                    ParseResult result = Parser.Parse(param.Query);
                    SyntaxParseResult syntaxResult = new SyntaxParseResult();
                    if (result != null && result.Errors.Count() == 0)
                    {
                        syntaxResult.Parseable = true;
                    } else
                    {
                        syntaxResult.Parseable = false;
                        string[] errorMessages = new string[result.Errors.Count()];
                        for (int i = 0; i < result.Errors.Count(); i++)
                        {
                            errorMessages[i] = result.Errors.ElementAt(i).Message;
                        }
                        syntaxResult.Errors = errorMessages;
                    }
                    await requestContext.SendResult(syntaxResult);
                }
                catch (Exception ex)
                {
                    await requestContext.SendError(ex.ToString());
                }
            });
        }

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
                        scriptFile.ClientUri,
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
                        isConnected = ConnectionServiceInstance.TryFindConnection(scriptFile.ClientUri, out connInfo);
                        definitionResult = GetDefinition(textDocumentPosition, scriptFile, connInfo);
                    }

                    if (definitionResult != null && !definitionResult.IsErrorResult)
                    {
                        await requestContext.SendResult(definitionResult.Locations);
                        succeeded = true;
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

        private async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {
            try
            {
                // check if Quick Info hover tooltips are enabled
                if (CurrentWorkspaceSettings.IsQuickInfoEnabled)
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
                await requestContext.SendResult(null);
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
        /// <param name="uri"></param>
        /// <param name="scriptFile"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        public async Task HandleDidOpenTextDocumentNotification(
            string uri,
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
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
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
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
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
                Logger.Write(TraceEventType.Verbose, "HandleRebuildIntelliSenseNotification");

                // Skip closing this file if the file doesn't exist
                var scriptFile = this.CurrentWorkspace.GetFile(rebuildParams.OwnerUri);
                if (scriptFile == null)
                {
                    return;
                }

                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    scriptFile.ClientUri,
                    out connInfo);

                // check that there is an active connection for the current editor
                if (connInfo != null)
                {
                    await Task.Run(() =>
                    {
                        // Get the current ScriptInfo if one exists so we can lock it while we're rebuilding the cache
                        ScriptParseInfo scriptInfo = GetScriptParseInfo(connInfo.OwnerUri, createIfNotExists: false);
                        if (scriptInfo != null && scriptInfo.IsConnected &&
                            Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                        {
                            try
                            {
                                _bindingQueue.AddConnectionContext(connInfo, true, featureName: "LanguageService", overwrite: true);
                                RemoveScriptParseInfo(rebuildParams.OwnerUri);
                                UpdateLanguageServiceOnConnection(connInfo).Wait();
                            }
                            catch (Exception ex)
                            {
                                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
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
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
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
                            await DiagnosticsHelper.ClearScriptDiagnostics(scriptFile.ClientUri, eventContext);
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
                Logger.Write(TraceEventType.Error, "Unknown error " + ex.ToString());
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
        /// Update the autocomplete metadata provider when the user connects to a database
        /// </summary>
        /// <param name="info"></param>
        public async Task UpdateLanguageServiceOnConnection(ConnectionInfo connInfo)
        {
            await Task.Run(() =>
            {
                if (ConnectionService.IsDedicatedAdminConnection(connInfo.ConnectionDetails))
                {
                    // Intellisense cannot be run on these connections as only 1 SqlConnection can be opened on them at a time
                    return;
                }
                ScriptParseInfo scriptInfo = GetScriptParseInfo(connInfo.OwnerUri, createIfNotExists: true);
                if (Monitor.TryEnter(scriptInfo.BuildingMetadataLock, LanguageService.OnConnectionWaitTimeout))
                {
                    try
                    {
                        scriptInfo.ConnectionKey = _bindingQueue.AddConnectionContext(connInfo, true,"languageService");
                        scriptInfo.IsConnected = _bindingQueue.IsBindingContextConnected(scriptInfo.ConnectionKey);
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(TraceEventType.Error, "Unknown error in OnConnection " + ex.ToString());
                        scriptInfo.IsConnected = false;
                    }
                    finally
                    {
                        // Set Metadata Build event to Signal state.
                        // (Tell Language Service that I am ready with Metadata Provider Object)
                        Monitor.Exit(scriptInfo.BuildingMetadataLock);
                    }
                }

                // TODOKusto: I dont think its required. Confirm later
                // PrepopulateCommonMetadata(connInfo, scriptInfo, this.BindingQueue);

                // Send a notification to signal that autocomplete is ready
                ServiceHostInstance.SendEvent(IntelliSenseReadyNotification.Type, new IntelliSenseReadyParams() {OwnerUri = connInfo.OwnerUri});
            });
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
        /// Resolves the details and documentation for a completion item. Move functionality to data source specific file when Language API supports description/details info.
        /// TODOKusto:Currently Kusto doesnt support getting the description details
        /// </summary>
        /// <param name="completionItem"></param>
        internal CompletionItem ResolveCompletionItem(CompletionItem completionItem)
        {
            return completionItem;
        }

        /// <summary>
        /// Get definition for a selected text from DataSource.
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptFile"></param>
        /// <param name="connInfo"></param>
        /// <returns> Location with the URI of the script file</returns>
        internal DefinitionResult GetDefinition(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ConnectionInfo connInfo)
        {
            // Parse sql
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(scriptFile.ClientUri);
            if (scriptParseInfo == null)
            {
                return null;
            }

            if (scriptParseInfo.IsConnected)
            {
                ReliableDataSourceConnection connection;
                connInfo.TryGetConnection("Default", out connection);
                IDataSource dataSource = connection.GetUnderlyingConnection();
                
                return dataSource.GetDefinition(scriptFile.Contents, textDocumentPosition.Position.Character, 1, 1);
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
        /// Get quick info hover tooltips for the current position
        /// </summary>
        /// <param name="textDocumentPosition"></param>
        /// <param name="scriptFile"></param>
        internal Hover GetHoverItem(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile)
        {
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(scriptFile.ClientUri);
            ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        scriptFile.ClientUri,
                        out connInfo);

            if (scriptParseInfo != null && scriptParseInfo.ParseResult != null)     // populate parseresult or check why it is used.
            {
                if (Monitor.TryEnter(scriptParseInfo.BuildingMetadataLock))
                {
                    try
                    {
                        QueueItem queueItem = _bindingQueue.QueueBindingOperation(
                            key: scriptParseInfo.ConnectionKey,
                            bindingTimeout: LanguageService.HoverTimeout,
                            bindOperation: (bindingContext, cancelToken) =>
                            {
                                // get the current quick info text
                                ScriptDocumentInfo scriptDocumentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
                                
                                ReliableDataSourceConnection connection;
                                connInfo.TryGetConnection("Default", out connection);
                                IDataSource dataSource = connection.GetUnderlyingConnection();               
                                
                                return dataSource.GetHoverHelp(scriptDocumentInfo, textDocumentPosition.Position);
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
            bool useLowerCaseSuggestions = this.CurrentWorkspaceSettings.SqlTools.IntelliSense.LowerCaseSuggestions.Value;

            // get the current script parse info object
            ScriptParseInfo scriptParseInfo = GetScriptParseInfo(scriptFile.ClientUri);

            if (scriptParseInfo == null)
            {
                var scriptDocInfo = ScriptDocumentInfo.CreateDefaultDocumentInfo(textDocumentPosition, scriptFile);
                resultCompletionItems = resultCompletionItems = _dataSourceFactory.GetDefaultAutoComplete(DataSourceType.Kusto, scriptDocInfo, textDocumentPosition.Position);       //TODO_KUSTO: DataSourceFactory.GetDefaultAutoComplete 1st param should get the datasource type generically instead of hard coded DataSourceType.Kusto
                return resultCompletionItems;
            }

            ScriptDocumentInfo scriptDocumentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);

            if(connInfo != null){
                ReliableDataSourceConnection connection;
                connInfo.TryGetConnection("Default", out connection);
                IDataSource dataSource = connection.GetUnderlyingConnection();
			    
                resultCompletionItems = dataSource.GetAutoCompleteSuggestions(scriptDocumentInfo, textDocumentPosition.Position);
            }
            else{
                resultCompletionItems = _dataSourceFactory.GetDefaultAutoComplete(DataSourceType.Kusto, scriptDocumentInfo, textDocumentPosition.Position);
            }

            // cache the current script parse info object to resolve completions later. Used for the detailed description.
            this.currentCompletionParseInfo = scriptParseInfo;


            // if the parse failed then return the default list
            if (scriptParseInfo.ParseResult == null)
            {
                resultCompletionItems = _dataSourceFactory.GetDefaultAutoComplete(DataSourceType.Kusto, scriptDocumentInfo, textDocumentPosition.Position);
                return resultCompletionItems;
            }

            // if there are no completions then provide the default list
            if (resultCompletionItems == null)          // this is the getting default keyword option when its not connected
            {
                resultCompletionItems = _dataSourceFactory.GetDefaultAutoComplete(DataSourceType.Kusto, scriptDocumentInfo, textDocumentPosition.Position);
            }

            return resultCompletionItems;
        }

        #endregion

        #region Diagnostic Provider methods

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
                Logger.Write(TraceEventType.Error, string.Format("Exception while cancelling analysis task:\n\n{0}", e.ToString()));

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
                    this.DelayedDiagnosticsTask = DelayThenInvokeDiagnostics(
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
                else if (ShouldSkipNonMssqlFile(scriptFile.ClientUri))
                {
                    // Clear out any existing markers in case file type was changed
                    await DiagnosticsHelper.ClearScriptDiagnostics(scriptFile.ClientUri, eventContext);
                    continue;
                }

                Logger.Write(TraceEventType.Verbose, "Analyzing script file: " + scriptFile.FilePath);

                // TODOKusto: Add file for mapping here, parity from parseAndbind function. Confirm it.
                ScriptParseInfo parseInfo = GetScriptParseInfo(scriptFile.ClientUri, createIfNotExists: true);

                ScriptFileMarker[] semanticMarkers = null;
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    scriptFile.ClientUri,
                    out connInfo);
                
                if(connInfo != null){
                    connInfo.TryGetConnection("Default", out var connection);
                    IDataSource dataSource = connection.GetUnderlyingConnection();
                    
                    semanticMarkers = dataSource.GetSemanticMarkers(parseInfo, scriptFile, scriptFile.Contents);
			    }
                else{
                    semanticMarkers = _dataSourceFactory.GetDefaultSemanticMarkers(DataSourceType.Kusto, parseInfo, scriptFile, scriptFile.Contents);
                }
                
                Logger.Write(TraceEventType.Verbose, "Analysis complete.");

                await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, semanticMarkers, eventContext);
            }
        }

        #endregion

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
            if (scriptFile != null && !string.IsNullOrWhiteSpace(scriptFile.ClientUri))
            {
                return scriptFile.ClientUri.StartsWith("tsqloutput:");
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
            if (_bindingQueue != null)
            {
                _bindingQueue.Dispose();
            }
        }
    }
}
