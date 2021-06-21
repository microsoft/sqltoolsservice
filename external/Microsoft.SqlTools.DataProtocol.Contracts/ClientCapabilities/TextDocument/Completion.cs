//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Common;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Capabilities specific to the 'textDocument/completion' request
    /// </summary>
    public class CompletionCapabilities : DynamicRegistrationCapability
    {
        /// <summary>
        /// Client supports these CompletionItem specific capabilities. Can be <c>null</c>
        /// </summary>
        public CompletionItemCapabilities CompletionItem { get; set; }
            
        /// <summary>
        /// Client supports these CompletionItemKinds as responses to completion requests
        /// </summary>
        public CompletionItemKindCapabiltities CompletionItemKind { get; set; }
    }
    
    public class CompletionItemCapabilities
    {
        /// <summary>
        /// Whether client supports snippet formats as completion results
        /// </summary>
        /// <remarks>
        /// A snippet can define tab stops and placeholders with <c>$1</c>, <c>$2</c> and
        /// <c>${3:foo}</c>. <c>$0</c> defines the final tab stop, it defaults to the end of
        /// the snippet. Placeholders with equal identifiers are linked, that is typing in one
        /// will update others, too.
        /// </remarks>
        public bool? SnippetSupport { get; set; }
            
        /// <summary>
        /// Whether client supports commit characters on a completion item
        /// </summary>
        public bool? CommitCharactersSpport { get; set; }
            
        /// <summary>
        /// Client supports these content formats for the documentation property. The order
        /// describes the preferred format of the client. May be <c>null</c>
        /// </summary>
        public MarkupKind[] DocumentationFormat { get; set; } 
    }
        
    public class CompletionItemKindCapabiltities
    {
        /// <summary>
        /// Completion item kind values the client supports. When this property exists, the
        /// client also guarantees that it will handle values outside its set gracefully and
        /// falls back to a default value when unknown.
        /// 
        /// If this property is not present, the client only supports the completion item kinds
        /// from Text to Reference as defined in the initial version of the protocol.
        /// </summary>
        public CompletionItemKinds? ValueSet { get; set; }
    }
}