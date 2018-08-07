//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Common;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Capabilities specific to textDocument/hover requests 
    /// </summary>
    public class HoverCapabilities : DynamicRegistrationCapability
    {            
        /// <summary>
        /// Client supports these content formats for the content property. The order describes
        /// the preferred format of the client.
        /// </summary>
        public MarkupKind[] ContentFormat { get; set; }
    }
}