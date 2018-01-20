//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Dmp.Contracts.Hosting
{
    /// <summary>
    /// VSCode language service capabilities
    /// See https://github.com/Microsoft/vscode-languageserver-node/ for more details
    /// </summary>
    public class LanguageServiceCapabilities
    {
        /// <summary>
        /// How the tools service accepts notifications to indicate open documents have changed
        /// </summary>
        public TextDocumentSyncKind? TextDocumentSync { get; set; }

        /// <summary>
        /// Whether or not this tools service provides text hover functionality
        /// </summary>
        public bool? HoverProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides auto-complete functionality
        /// </summary>
        public CompletionOptions CompletionProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides tool tips for method, etc signatures
        /// </summary>
        public SignatureHelpOptions SignatureHelpProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides method, etc definitions
        /// </summary>
        public bool? DefinitionProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides lists of project references for tokens
        /// </summary>
        public bool? ReferencesProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides document highlighting functionality
        /// </summary>
        public bool? DocumentHighlightProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides document formatting functionality
        /// </summary>
        public bool? DocumentFormattingProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides text selection formatting functionality
        /// </summary>
        public bool? DocumentRangeFormattingProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides document-scoped symbol functionality
        /// </summary>
        public bool? DocumentSymbolProvider { get; set; }

        /// <summary>
        /// Whether or not this tools service provides workspace-scoped symbol functionality
        /// </summary>
        public bool? WorkspaceSymbolProvider { get; set; }
    }

    /// <summary>
    /// Defines the document synchronization strategies that a server may support.
    /// </summary>
    public enum TextDocumentSyncKind
    {
        /// <summary>
        /// Indicates that documents should not be synced at all.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that document changes are always sent with the full content.
        /// </summary>
        Full,

        /// <summary>
        /// Indicates that document changes are sent as incremental changes after
        /// the initial document content has been sent.
        /// </summary>
        Incremental
    }

    public class CompletionOptions
    {
        public bool? ResolveProvider { get; set; }

        public string[] TriggerCharacters { get; set; }
    }

    public class SignatureHelpOptions
    {
        public string[] TriggerCharacters { get; set; }
    }
}

