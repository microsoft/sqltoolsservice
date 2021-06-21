//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.TextDocument;
using Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.Workspace;

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities
{
    public class ClientCapabilities
    {   
        /// <summary>
        /// Any experimental client capabilities
        /// </summary>
        public object Experimental { get; set; } 
    
        /// <summary>
        /// Text document specific client capabilities, can be null
        /// </summary>
        public TextDocumentCapabilities TextDocument { get; set; }
    
        /// <summary>
        /// Workspace specific client capabilities, can be null
        /// </summary>
        public WorkspaceCapabilities Workspace { get; set; }
    }
}