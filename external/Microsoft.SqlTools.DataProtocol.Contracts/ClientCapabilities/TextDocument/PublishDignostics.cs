//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Capabilities specific to textDocument/publishDiagnostics requests
    /// </summary>
    public class PublishDignosticsCapabilities
    {
        /// <summary>
        /// Whether the client accepts diagnostics with related information
        /// </summary>
        public bool? RelatedInformation { get; set; }
    }
}