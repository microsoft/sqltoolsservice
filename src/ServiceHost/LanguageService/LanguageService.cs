//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Session;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.LanguageService.Contracts;
using Microsoft.SqlTools.ServiceLayer.ServiceHost.Protocol;
using Microsoft.SqlTools.ServiceLayer.WorkspaceService.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageService
{
    /// <summary>
    /// Main class for Language Service functionality
    /// </summary>
    public class LanguageService
    {

        #region Singleton Instance Implementation

        private static LanguageService instance;

        public static LanguageService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LanguageService();
                }
                return instance;
            }
        }

        /// <summary>
        /// Constructor for the Language Service class
        /// </summary>
        /// <param name="context"></param>
        private LanguageService(SqlToolsContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Default, parameterless contstructor.
        /// TODO: Remove once the SqlToolsContext stuff is sorted out
        /// </summary>
        private LanguageService()
        {
            
        }

        #endregion

        /// <summary>
        /// Gets or sets the current SQL Tools context
        /// </summary>
        /// <returns></returns>
        private SqlToolsContext Context { get; set; }

        public void InitializeService(ServiceHost.ServiceHost serviceHost)
        {
            // Register the requests that this service will handle
            serviceHost.SetRequestHandler(DefinitionRequest.Type, HandleDefinitionRequest);
            serviceHost.SetRequestHandler(ReferencesRequest.Type, HandleReferencesRequest);
            serviceHost.SetRequestHandler(CompletionRequest.Type, HandleCompletionRequest);
            serviceHost.SetRequestHandler(CompletionResolveRequest.Type, HandleCompletionResolveRequest);
            serviceHost.SetRequestHandler(SignatureHelpRequest.Type, HandleSignatureHelpRequest);
            serviceHost.SetRequestHandler(DocumentHighlightRequest.Type, HandleDocumentHighlightRequest);
            serviceHost.SetRequestHandler(HoverRequest.Type, HandleHoverRequest);
            serviceHost.SetRequestHandler(DocumentSymbolRequest.Type, HandleDocumentSymbolRequest);
            serviceHost.SetRequestHandler(WorkspaceSymbolRequest.Type, HandleWorkspaceSymbolRequest);

            // Register a no-op shutdown task for validation of the shutdown logic
            serviceHost.RegisterShutdownTask(async (shutdownParams, shutdownRequestContext) =>
            {
                Logger.Write(LogLevel.Verbose, "Shutting down language service");
                await Task.FromResult(0);
            });
        }

        #region Request Handlers

        private static async Task HandleDefinitionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDefinitionRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleReferencesRequest(
            ReferencesParams referencesParams,
            RequestContext<Location[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleReferencesRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleCompletionRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<CompletionItem[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleCompletionRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleCompletionResolveRequest(
            CompletionItem completionItem,
            RequestContext<CompletionItem> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleCompletionResolveRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleSignatureHelpRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<SignatureHelp> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleSignatureHelpRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleDocumentHighlightRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<DocumentHighlight[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDocumentHighlightRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleHoverRequest(
            TextDocumentPosition textDocumentPosition,
            RequestContext<Hover> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleHoverRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleDocumentSymbolRequest(
            TextDocumentIdentifier textDocumentIdentifier,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDocumentSymbolRequest");
            await Task.FromResult(true);
        }

        private static async Task HandleWorkspaceSymbolRequest(
            WorkspaceSymbolParams workspaceSymbolParams,
            RequestContext<SymbolInformation[]> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleWorkspaceSymbolRequest");
            await Task.FromResult(true);
        }

        #endregion

        /// <summary>
        /// Gets a list of semantic diagnostic marks for the provided script file
        /// </summary>
        /// <param name="scriptFile"></param>
        public ScriptFileMarker[] GetSemanticMarkers(ScriptFile scriptFile)
        {
            // the commented out snippet is an example of how to create a error marker
            // semanticMarkers = new ScriptFileMarker[1];
            // semanticMarkers[0] = new ScriptFileMarker()
            // {
            //     Message = "Error message",
            //     Level = ScriptFileMarkerLevel.Error,
            //     ScriptRegion = new ScriptRegion()
            //     {
            //         File = scriptFile.FilePath,
            //         StartLineNumber = 2,
            //         StartColumnNumber = 2,  
            //         StartOffset = 0,
            //         EndLineNumber = 4,
            //         EndColumnNumber = 10,
            //         EndOffset = 0
            //     }
            // };
            return new ScriptFileMarker[0];
        }
    }
}
