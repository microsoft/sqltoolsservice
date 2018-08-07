//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.Workspace
{
    /// <summary>
    /// Workspace specific client capabilities
    /// </summary>
    public class WorkspaceCapabilities
    {
        /// <summary>
        /// Client support applying batche edits to the workspace by supporting the
        /// workspace/applyEdit request 
        /// </summary>
        public bool? ApplyEdit { get; set; }
        
        /// <summary>
        /// Whether the client supports workspace/configuration requests
        /// </summary>
        public bool? Configuration { get; set; }
        
        /// <summary>
        /// Capabilities specific to the workspace/didChangeConfiguration notification. Can be
        /// <c>null</c>
        /// </summary>
        public DidChangeConfigurationCapabilities DidChangeConfiguration { get; set; }
        
        /// <summary>
        /// Capabilities specific to the workspace/executeCommand request. Can be <c>null</c>
        /// </summary>
        public ExecuteCommandCapabilities ExecuteCommand { get; set; }
        
        /// <summary>
        /// Capabilities specific to the workspace/didChangeWatchedFiles notificiation. Can be
        /// <c>null</c>
        /// </summary>
        public DidChangeWatchedFilesCapabilities DidChangeWatchedFiles { get; set; }

        /// <summary>
        /// Capabilities specific to the workspace/symbol request. Can be <c>null</c>
        /// </summary>
        public SymbolCapabilities Symbol { get; set; }
        
        /// <summary>
        /// Capabilities specific to WorkspaceEdit requests
        /// </summary>
        public WorkspaceEditCapabilities WorkspaceEdit { get; set; }
        
        /// <summary>
        /// Whether the client supports multiple workspace folders open at a time. If true, the
        /// open workspace folders will be provided during initialization via
        /// <see cref="InitializeRequest{TInitializationOptions}.WorkspaceFolders"/> 
        /// </summary>
        public bool? WorkspaceFolders { get; set; }
    }
}