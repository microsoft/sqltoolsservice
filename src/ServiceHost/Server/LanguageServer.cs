//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.EditorServices.Protocol.LanguageServer;
using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol;
using Microsoft.SqlTools.EditorServices.Protocol.MessageProtocol.Channel;
using Microsoft.SqlTools.EditorServices.Session;
using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Utility;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System;

namespace Microsoft.SqlTools.EditorServices.Protocol.Server
{
    /// <summary>
    /// SQL Tools VS Code Language Server request handler
    /// </summary>
    public class LanguageServer : LanguageServerBase
    {
        private static CancellationTokenSource existingRequestCancellation;

        private LanguageServerSettings currentSettings = new LanguageServerSettings();
        
        private EditorSession editorSession;

        /// <param name="hostDetails">
        /// Provides details about the host application.
        /// </param>
        public LanguageServer(HostDetails hostDetails, ProfilePaths profilePaths)
            : base(new StdioServerChannel())
        {
            this.editorSession = new EditorSession();
            this.editorSession.StartSession(hostDetails, profilePaths);
        }

        /// <summary>
        /// Initialize the VS Code request/response callbacks
        /// </summary>
        protected override void Initialize()
        {
            // Register all supported message types
            this.SetRequestHandler(InitializeRequest.Type, this.HandleInitializeRequest);
            this.SetEventHandler(DidChangeTextDocumentNotification.Type, this.HandleDidChangeTextDocumentNotification);
            this.SetEventHandler(DidOpenTextDocumentNotification.Type, this.HandleDidOpenTextDocumentNotification);
            this.SetEventHandler(DidCloseTextDocumentNotification.Type, this.HandleDidCloseTextDocumentNotification);
            this.SetEventHandler(DidChangeConfigurationNotification<LanguageServerSettingsWrapper>.Type, this.HandleDidChangeConfigurationNotification);

            this.SetRequestHandler(DefinitionRequest.Type, this.HandleDefinitionRequest);
            this.SetRequestHandler(ReferencesRequest.Type, this.HandleReferencesRequest);
            this.SetRequestHandler(CompletionRequest.Type, this.HandleCompletionRequest);
            this.SetRequestHandler(CompletionResolveRequest.Type, this.HandleCompletionResolveRequest);
            this.SetRequestHandler(SignatureHelpRequest.Type, this.HandleSignatureHelpRequest);
            this.SetRequestHandler(DocumentHighlightRequest.Type, this.HandleDocumentHighlightRequest);
            this.SetRequestHandler(HoverRequest.Type, this.HandleHoverRequest);
            this.SetRequestHandler(DocumentSymbolRequest.Type, this.HandleDocumentSymbolRequest);
            this.SetRequestHandler(WorkspaceSymbolRequest.Type, this.HandleWorkspaceSymbolRequest);               
        }

        /// <summary>
        /// Handles the shutdown event for the Language Server
        /// </summary>
        protected override async Task Shutdown()
        {
            Logger.Write(LogLevel.Normal, "Language service is shutting down...");

            if (this.editorSession != null)
            {
                this.editorSession.Dispose();
                this.editorSession = null;
            }

            await Task.FromResult(true);
        }

        /// <summary>
        /// Handles the initialization request
        /// </summary>
        /// <param name="initializeParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        protected async Task HandleInitializeRequest(
            InitializeRequest initializeParams,
            RequestContext<InitializeResult> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidChangeTextDocumentNotification");

            // Grab the workspace path from the parameters
           editorSession.Workspace.WorkspacePath = initializeParams.RootPath;

            await requestContext.SendResult(
                new InitializeResult
                {
                    Capabilities = new ServerCapabilities
                    {
                        TextDocumentSync = TextDocumentSyncKind.Incremental,
                        DefinitionProvider = true,
                        ReferencesProvider = true,
                        DocumentHighlightProvider = true,
                        DocumentSymbolProvider = true,
                        WorkspaceSymbolProvider = true,
                        HoverProvider = true,
                        CompletionProvider = new CompletionOptions
                        {
                            ResolveProvider = true,
                            TriggerCharacters = new string[] { ".", "-", ":", "\\" }
                        },
                        SignatureHelpProvider = new SignatureHelpOptions
                        {
                            TriggerCharacters = new string[] { " " } // TODO: Other characters here?
                        }
                    }
                });
        }

        /// <summary>
        /// Handles text document change events
        /// </summary>
        /// <param name="textChangeParams"></param>
        /// <param name="eventContext"></param>
        /// <returns></returns>
        protected async Task HandleDidChangeTextDocumentNotification(
            DidChangeTextDocumentParams textChangeParams,
            EventContext eventContext)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append("HandleDidChangeTextDocumentNotification"); 
            List<ScriptFile> changedFiles = new List<ScriptFile>();

            // A text change notification can batch multiple change requests
            foreach (var textChange in textChangeParams.ContentChanges)
            {
                string fileUri = textChangeParams.Uri ?? textChangeParams.TextDocument.Uri;
                msg.AppendLine();
                msg.Append("  File: ");
                msg.Append(fileUri);

                ScriptFile changedFile = editorSession.Workspace.GetFile(fileUri);

                changedFile.ApplyChange(
                    GetFileChangeDetails(
                        textChange.Range.Value,
                        textChange.Text));

                changedFiles.Add(changedFile);
            }

            Logger.Write(LogLevel.Verbose, msg.ToString());

            await this.RunScriptDiagnostics(
                changedFiles.ToArray(),
                editorSession,
                eventContext);

            await Task.FromResult(true);
        }

        protected Task HandleDidOpenTextDocumentNotification(
            DidOpenTextDocumentNotification openParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidOpenTextDocumentNotification");
            return Task.FromResult(true);
        }

         protected Task HandleDidCloseTextDocumentNotification(
            TextDocumentIdentifier closeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidCloseTextDocumentNotification");
            return Task.FromResult(true);
        }

        /// <summary>
        /// Handles the configuration change event
        /// </summary>
        /// <param name="configChangeParams"></param>
        /// <param name="eventContext"></param>
        protected async Task HandleDidChangeConfigurationNotification(
            DidChangeConfigurationParams<LanguageServerSettingsWrapper> configChangeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDidChangeConfigurationNotification");

            bool oldLoadProfiles = this.currentSettings.EnableProfileLoading;
            bool oldScriptAnalysisEnabled = 
                this.currentSettings.ScriptAnalysis.Enable.HasValue;
            string oldScriptAnalysisSettingsPath =
                this.currentSettings.ScriptAnalysis.SettingsPath;

            this.currentSettings.Update(
                configChangeParams.Settings.SqlTools, 
                this.editorSession.Workspace.WorkspacePath);

            // If script analysis settings have changed we need to clear & possibly update the current diagnostic records.
            if ((oldScriptAnalysisEnabled != this.currentSettings.ScriptAnalysis.Enable))
            {
                // If the user just turned off script analysis or changed the settings path, send a diagnostics
                // event to clear the analysis markers that they already have.
                if (!this.currentSettings.ScriptAnalysis.Enable.Value)
                {
                    ScriptFileMarker[] emptyAnalysisDiagnostics = new ScriptFileMarker[0];

                    foreach (var scriptFile in editorSession.Workspace.GetOpenedFiles())
                    {
                        await PublishScriptDiagnostics(
                            scriptFile,
                            emptyAnalysisDiagnostics,
                            eventContext);
                    }
                }
                else
                {
                    await this.RunScriptDiagnostics(
                        this.editorSession.Workspace.GetOpenedFiles(),
                        this.editorSession,
                        eventContext);
                }
            }

            await Task.FromResult(true);
        }

        protected async Task HandleDefinitionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDefinitionRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleReferencesRequest(
            ReferencesParams referencesParams,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleReferencesRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleCompletionRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleCompletionResolveRequest(
            CompletionItem completionItem,
            RequestContext<CompletionItem> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleCompletionResolveRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleSignatureHelpRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<SignatureHelp> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleSignatureHelpRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleDocumentHighlightRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<DocumentHighlight[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDocumentHighlightRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleHoverRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleDocumentSymbolRequest(
            TextDocumentIdentifier textDocumentIdentifier,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDocumentSymbolRequest");
            await Task.FromResult(true);     
        }

        protected async Task HandleWorkspaceSymbolRequest(
            WorkspaceSymbolParams workspaceSymbolParams,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleWorkspaceSymbolRequest");
            await Task.FromResult(true);
        }

        /// <summary>
        /// Runs script diagnostics on changed files
        /// </summary>
        /// <param name="filesToAnalyze"></param>
        /// <param name="editorSession"></param>
        /// <param name="eventContext"></param>
        private Task RunScriptDiagnostics(
            ScriptFile[] filesToAnalyze,
            EditorSession editorSession,
            EventContext eventContext)
        {
            if (!this.currentSettings.ScriptAnalysis.Enable.Value)
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
            existingRequestCancellation = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                    DelayThenInvokeDiagnostics(
                        750,
                        filesToAnalyze,
                        editorSession,
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
        /// <param name="editorSession"></param>
        /// <param name="eventContext"></param>
        /// <param name="cancellationToken"></param>
        private static async Task DelayThenInvokeDiagnostics(
            int delayMilliseconds,
            ScriptFile[] filesToAnalyze,
            EditorSession editorSession,
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
                ScriptFileMarker[] semanticMarkers = null;
                if (editorSession.LanguageService != null)
                {
                    Logger.Write(LogLevel.Verbose, "Analyzing script file: " + scriptFile.FilePath);
                    semanticMarkers = editorSession.LanguageService.GetSemanticMarkers(scriptFile);
                    Logger.Write(LogLevel.Verbose, "Analysis complete.");
                }
                else
                {
                    // Semantic markers aren't available if the AnalysisService
                    // isn't available
                    semanticMarkers = new ScriptFileMarker[0];                   
                }

                await PublishScriptDiagnostics(
                    scriptFile,
                    semanticMarkers,
                    eventContext);
            }
        }

        /// <summary>
        /// Send the diagnostic results back to the host application
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="semanticMarkers"></param>
        /// <param name="eventContext"></param>
        private static async Task PublishScriptDiagnostics(
            ScriptFile scriptFile,
            ScriptFileMarker[] semanticMarkers,
            EventContext eventContext)
        {
            var allMarkers = scriptFile.SyntaxMarkers != null 
                    ? scriptFile.SyntaxMarkers.Concat(semanticMarkers)
                    : semanticMarkers;

            // Always send syntax and semantic errors.  We want to 
            // make sure no out-of-date markers are being displayed.
            await eventContext.SendEvent(
                PublishDiagnosticsNotification.Type,
                new PublishDiagnosticsNotification
                {
                    Uri = scriptFile.ClientFilePath,
                    Diagnostics =
                       allMarkers
                            .Select(GetDiagnosticFromMarker)
                            .ToArray()
                });
        }
        
        /// <summary>
        /// Convert a ScriptFileMarker to a Diagnostic that is Language Service compatible
        /// </summary>
        /// <param name="scriptFileMarker"></param>
        /// <returns></returns>
        private static Diagnostic GetDiagnosticFromMarker(ScriptFileMarker scriptFileMarker)
        {
            return new Diagnostic
            {
                Severity = MapDiagnosticSeverity(scriptFileMarker.Level),
                Message = scriptFileMarker.Message,
                Range = new Range
                {
                    // TODO: What offsets should I use?
                    Start = new Position
                    {
                        Line = scriptFileMarker.ScriptRegion.StartLineNumber - 1,
                        Character = scriptFileMarker.ScriptRegion.StartColumnNumber - 1
                    },
                    End = new Position
                    {
                        Line = scriptFileMarker.ScriptRegion.EndLineNumber - 1,
                        Character = scriptFileMarker.ScriptRegion.EndColumnNumber - 1
                    }
                }
            };
        }

        /// <summary>
        /// Map ScriptFileMarker severity to Diagnostic severity
        /// </summary>
        /// <param name="markerLevel"></param>        
        private static DiagnosticSeverity MapDiagnosticSeverity(ScriptFileMarkerLevel markerLevel)
        {
            switch (markerLevel)
            {
                case ScriptFileMarkerLevel.Error:
                    return DiagnosticSeverity.Error;

                case ScriptFileMarkerLevel.Warning:
                    return DiagnosticSeverity.Warning;

                case ScriptFileMarkerLevel.Information:
                    return DiagnosticSeverity.Information;

                default:
                    return DiagnosticSeverity.Error;
            }
        }

        /// <summary>
        /// Switch from 0-based offsets to 1 based offsets
        /// </summary>
        /// <param name="changeRange"></param>
        /// <param name="insertString"></param>        
        private static FileChange GetFileChangeDetails(Range changeRange, string insertString)
        {
            // The protocol's positions are zero-based so add 1 to all offsets
            return new FileChange
            {
                InsertString = insertString,
                Line = changeRange.Start.Line + 1,
                Offset = changeRange.Start.Character + 1,
                EndLine = changeRange.End.Line + 1,
                EndOffset = changeRange.End.Character + 1
            };
        }
    }
}
