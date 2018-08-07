//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ClientCapabilities.Workspace
{
    /// <summary>
    /// Capabilities specific to WorkspaceEdit requests
    /// </summary>
    public class WorkspaceEditCapabilities
    {
        /// <summary>
        /// Whether the client supports versioned document changes in WorkspaceEdit requests
        /// </summary>
        public bool? DocumentChanges { get; set; }
    }
}