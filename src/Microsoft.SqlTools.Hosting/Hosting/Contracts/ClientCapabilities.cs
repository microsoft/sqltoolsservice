//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines a class that describes the capabilities of a language client (editor).
    /// Only the subset of capabilities the language service reacts to is modelled;
    /// any other capabilities sent by the client are ignored during deserialization.
    /// </summary>
    public class ClientCapabilities
    {
        /// <summary>
        /// Gets or sets the text-document scoped capabilities of the client.
        /// </summary>
        public TextDocumentClientCapabilities TextDocument { get; set; }
    }

    /// <summary>
    /// Text-document scoped client capabilities.
    /// </summary>
    public class TextDocumentClientCapabilities
    {
        /// <summary>
        /// Gets or sets the client's pull-model diagnostic capabilities. When this
        /// is present the client supports pulling diagnostics via
        /// <c>textDocument/diagnostic</c>, and the language service suppresses the
        /// legacy push (<c>textDocument/publishDiagnostics</c>) path to avoid
        /// reporting the same diagnostics twice.
        /// </summary>
        public DiagnosticClientCapabilities Diagnostic { get; set; }
    }

    /// <summary>
    /// Client capabilities specific to pull-model diagnostics.
    /// </summary>
    public class DiagnosticClientCapabilities
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client supports related
        /// documents for diagnostic pulls.
        /// </summary>
        public bool RelatedDocumentSupport { get; set; }
    }
}

