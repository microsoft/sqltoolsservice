//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{    
    public class ServerCapabilities
    {
        /// <summary>
        /// Defines how text documents are synced. Is either a detailed structure defining each
        /// notification or for backwards compatibility the TextDocumentSyncKind number.
        /// </summary>
        /// TODO: Use the Union type
        public TextDocumentSyncKind TextDocumentSync { get; set; }
        
        /// <summary>
        /// Whether the server provides code actions. Can be <c>null</c>
        /// </summary>
        public bool? CodeActionProvider { get; set; }
        
        /// <summary>
        /// Options that the server provides for code lens. Can be <c>null</c> to indicate code
        /// lens is not supported.
        /// </summary>
        public CodeLensOptions CodeLensProvider { get; set; }
        
        /// <summary>
        /// Options that the server supports for completion. Can be <c>null</c> to indicate
        /// completion is not supported.
        /// </summary>
        public CompletionOptions CompletionProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides goto definition support. Can be <c>null</c>
        /// </summary>
        public bool? DefinitionProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides document formatting. Can be <c>null</c>
        /// </summary>
        public bool? DocumentFormattingProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides document highlight support. Can be <c>null</c>
        /// </summary>
        public bool? DocumentHighlightProvider { get; set; }
        
        /// <summary>
        /// Options the server supports for document linking. Can be <c>null</c> to indicate the
        /// feature is not supported
        /// </summary>
        public DocumentLinkOptions DocumentLinkProvider { get; set; }
        
        /// <summary>
        /// Options that the server supports for document formatting on type. Can be <c>null</c> to
        /// indicate the feature is not supported
        /// </summary>
        public DocumentOnTypeFormattingOptions DocumentOnTypeFormattingProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides document symbol support. Can be <c>null</c>
        /// </summary>
        public bool? DocumentSymbolProvider { get; set; }
        
        /// <summary>
        /// Options the server supports for executing commands. Can be <c>null</c> to indicate the
        /// feature is not supported.
        /// </summary>
        public ExecuteCommandOptions ExecuteCommandProvider { get; set; }
        
        /// <summary>
        /// Any experimental features the server supports
        /// </summary>
        /// TODO: Should this be a parameterized type?
        public object Experimental { get; set; }
        
        /// <summary>
        /// Whether or not the server supports goto implementation requests. Can be <c>null</c> 
        /// </summary>
        /// TODO: Union type
        public bool? ImplementationProvider { get; set; } 
        
        /// <summary>
        /// Whether the server provides hover support. Can be <c>null</c>
        /// </summary>
        public bool? HoverProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides find references support. Can be <c>null</c>
        /// </summary>
        public bool? ReferencesProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides support for renaming. Can be <c>null</c>
        /// </summary>
        public bool? RenameProvider { get; set; }
        
        /// <summary>
        /// Options that the server supports for signature help. Can be <c>null</c> to indicate
        /// completion is not supported
        /// </summary>
        public SignatureHelpOptions SignatureHelpProvider { get; set; }
        
        /// <summary>
        /// Whether the server provides goto type definition support. Can be <c>null</c>
        /// </summary>
        /// TODO: Union type
        public bool? TypeDefinitionProvider { get; set; }
        
        /// <summary>
        /// Options specific to workspaces the server supoorts. Can be <c>null</c> to indicate the
        /// server does not support workspace requests.
        /// </summary>
        public WorkspaceCapabilities Workspace { get; set; }
        
        /// <summary>
        /// Whether the server provides workpace symbol support. Can be <c>null</c>
        /// </summary>
        public bool? WorkspaceSymbolProvider { get; set; }
    }
}