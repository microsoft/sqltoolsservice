//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Common;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Capabilities specific to textDocument/signatureHelp requests
    /// </summary>
    public class SignatureHelpCapabilities : DynamicRegistrationCapability
    {           
        /// <summary>
        /// Client supports these SignatureInformation specific properties
        /// </summary>
        public SignatureInformationCapabilities SignatureInformation { get; set; }
    }
        
    public class SignatureInformationCapabilities
    {
        /// <summary>
        /// Client supports these content formats for the documentation property. The order
        /// describes the preferred format of the client.
        /// </summary>
        public MarkupKind[] DocumentFormat { get; set; }
    }
}