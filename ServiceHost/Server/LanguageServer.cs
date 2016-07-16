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

namespace Microsoft.SqlTools.EditorServices.Protocol.Server
{
    public class LanguageServer : LanguageServerBase
    {
        /// <param name="hostDetails">
        /// Provides details about the host application.
        /// </param>
        public LanguageServer(HostDetails hostDetails, ProfilePaths profilePaths)
            : base(new StdioServerChannel())
        {
        }

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

        protected override async Task Shutdown()
        {
            await Task.FromResult(true);
        }

        protected async Task HandleInitializeRequest(
            InitializeRequest initializeParams,
            RequestContext<InitializeResult> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDidChangeTextDocumentNotification");

            // Grab the workspace path from the parameters
           //editorSession.Workspace.WorkspacePath = initializeParams.RootPath;

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

        protected Task HandleDidChangeTextDocumentNotification(
            DidChangeTextDocumentParams textChangeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDidChangeTextDocumentNotification");
            return Task.FromResult(true);
        }

        protected Task HandleDidOpenTextDocumentNotification(
            DidOpenTextDocumentNotification openParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDidOpenTextDocumentNotification");
            return Task.FromResult(true);
        }

         protected Task HandleDidCloseTextDocumentNotification(
            TextDocumentIdentifier closeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDidCloseTextDocumentNotification");
            return Task.FromResult(true);
        }

        protected async Task HandleDidChangeConfigurationNotification(
            DidChangeConfigurationParams<LanguageServerSettingsWrapper> configChangeParams,
            EventContext eventContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDidChangeConfigurationNotification");
            await Task.FromResult(true);
        }

        protected async Task HandleDefinitionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDefinitionRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleReferencesRequest(
            ReferencesParams referencesParams,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleReferencesRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleCompletionRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleCompletionResolveRequest(
            CompletionItem completionItem,
            RequestContext<CompletionItem> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleCompletionResolveRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleSignatureHelpRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<SignatureHelp> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleSignatureHelpRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleDocumentHighlightRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<DocumentHighlight[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDocumentHighlightRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleHoverRequest");
            await Task.FromResult(true);
        }

        protected async Task HandleDocumentSymbolRequest(
            TextDocumentIdentifier textDocumentIdentifier,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleDocumentSymbolRequest");
            await Task.FromResult(true);     
        }

        protected async Task HandleWorkspaceSymbolRequest(
            WorkspaceSymbolParams workspaceSymbolParams,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Normal, "HandleWorkspaceSymbolRequest");
            await Task.FromResult(true);
        }    
    }
}
