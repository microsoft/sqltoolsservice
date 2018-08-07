//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.Common;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument
{
    /// <summary>
    /// Capabilities specific to textDocument/documentSymbol requests
    /// </summary>
    public class DocumentSymbolCapabilities : DynamicRegistrationCapability
    {           
        /// <summary>
        /// Specific capabilities for the SymbolKind
        /// </summary>
        public DocumentSymbolKindCapabilities SymbolKind { get; set; }
    }
        
    public class DocumentSymbolKindCapabilities
    {
        /// <summary>
        /// Symbol kind values the client supports. When this property exists, the client also
        /// guarantees that it will handle values outside its set gracefully and falls back to a
        /// default value when unknown.
        /// 
        /// If this property is not present, the client only supports the symbol kinds from File to
        /// Array as defined in the initial version of the protocol
        /// </summary>
        public SymbolKinds? ValueSet { get; set; }
    }
}