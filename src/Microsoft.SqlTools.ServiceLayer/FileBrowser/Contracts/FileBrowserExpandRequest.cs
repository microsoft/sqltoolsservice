//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Response from expanding a node
    /// </summary>
    public class FileBrowserExpandResponse
    {
        /// <summary>
        /// Expanded node 
        /// </summary>
        public FileTreeNode ExpandedNode;

        /// <summary>
        /// Result of the operation
        /// </summary>
        public bool Result;

        /// <summary>
        /// Error message if any
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// Request to expand a node in the file browser
    /// </summary>
    public class FileBrowserExpandRequest
    {
        public static readonly
            RequestType<FileBrowserParams, FileBrowserExpandResponse> Type =
                RequestType<FileBrowserParams, FileBrowserExpandResponse>.Create("filebrowser/expandnode");
    }
}