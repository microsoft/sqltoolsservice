//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Text document specific client capabilities
    /// </summary>
    public class TextDocumentCapabilities
    {
        /// <summary>
        /// Capabilities specific to the textDocument/codeAction request. Can be <c>null</c>
        /// </summary>
        public CodeActionCapabilities CodeAction { get; set; }
        
        /// <summary>
        /// Capabilities specific to the colorProvider. Can be <c>null</c>
        /// </summary>
        public ColorProviderCapabilities ColorProvider { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/completion request. Can be <c>null</c>
        /// </summary>
        public CompletionCapabilities Completion { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/definition request. Can be <c>null</c>
        /// </summary>
        public DefinitionCapabilities Definition { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/highlight request. Can be <c>null</c>
        /// </summary>
        public DocumentHighlightCapabilities DocumentHighlight { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/documentLink request. Can be <c>null</c>
        /// </summary>
        public DocumentLinkCapabilities DocumentLink { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/documentSymbol request. Can be <c>null</c>
        /// </summary>
        public DocumentSymbolCapabilities DocumentSymbol { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/formatting request. Can be <c>null</c>
        /// </summary>
        public FormattingCapabilities Formatting { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/hover request. Can be <c>null</c>
        /// </summary>
        public HoverCapabilities Hover { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/implementation request. Can be <c>null</c>
        /// </summary>
        public ImplementationCapabilities Implementation { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/onTypeFormatting request. Can be <c>null</c>
        /// </summary>
        public OnTypeFormattingCapabilities OnTypeFormatting { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/publishDiagnostics request. Can be <c>null</c>
        /// </summary>
        public PublishDignosticsCapabilities PublishDiagnostics { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/rangeFormatting request. Can be <c>null</c>
        /// </summary>
        public RangeFormattingCapabilities RangeFormatting { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/references request. Can be <c>null</c>
        /// </summary>
        public ReferencesCapabilities References { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/rename request. Can be <c>null</c>
        /// </summary>
        public RenameCapabilities Rename { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/signatureHelp request. Can be <c>null</c>
        /// </summary>
        public SignatureHelpCapabilities SignatureHelp { get; set; }
        
        /// <summary>
        /// Defines which synchronization capabilities the client supports. Can be <c>null</c>
        /// </summary>
        public SynchronizationCapabilities Synchronization { get; set; }
        
        /// <summary>
        /// Capabilities specific to the textDocument/typeDefinition requests. Can be <c>null</c>
        /// </summary>
        public TypeDefinitionCapabilities TypeDefinition { get; set; }
    }
}