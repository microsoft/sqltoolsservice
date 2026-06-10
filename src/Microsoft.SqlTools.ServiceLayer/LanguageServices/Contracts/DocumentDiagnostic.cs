//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts
{
    /// <summary>
    /// Parameters for a pull-model <see cref="DocumentDiagnosticRequest"/>.
    /// </summary>
    public class DocumentDiagnosticParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }

        /// <summary>
        /// Gets or sets the result id of a previous response, if any. The
        /// language service always returns a full report so this is ignored.
        /// </summary>
        public string PreviousResultId { get; set; }
    }

    /// <summary>
    /// A full document diagnostic report containing the complete set of
    /// diagnostics for a document. The <see cref="Kind"/> discriminator is
    /// always "full" as required by the language server protocol.
    /// </summary>
    public class FullDocumentDiagnosticReport
    {
        /// <summary>
        /// Gets the report kind discriminator. Always "full".
        /// </summary>
        public string Kind { get; } = "full";

        /// <summary>
        /// Gets or sets an optional result id for this report.
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// Gets or sets the diagnostics for the document.
        /// </summary>
        public Diagnostic[] Items { get; set; }
    }

    /// <summary>
    /// Pull-model diagnostics request for a single document.
    /// </summary>
    public class DocumentDiagnosticRequest
    {
        public static readonly
            RequestType<DocumentDiagnosticParams, FullDocumentDiagnosticReport> Type =
            RequestType<DocumentDiagnosticParams, FullDocumentDiagnosticReport>.Create("textDocument/diagnostic");
    }
}
