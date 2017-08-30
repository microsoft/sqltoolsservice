//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters for expanding a folder node
    /// </summary>
    public class FileBrowserExpandParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// The path to expand the nodes for
        /// </summary>
        public string ExpandPath;
    }

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
        public bool Succeeded;

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
            RequestType<FileBrowserExpandParams, FileBrowserExpandResponse> Type =
                RequestType<FileBrowserExpandParams, FileBrowserExpandResponse>.Create("filebrowser/expandnode");
    }
}