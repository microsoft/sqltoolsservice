//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Event params for expanding a node
    /// </summary>
    public class FileBrowserExpandCompleteParams
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
    /// Notification for expand completion
    /// </summary>
    public class FileBrowserExpandCompleteNotification
    {
        public static readonly
            EventType<FileBrowserExpandCompleteParams> Type =
            EventType<FileBrowserExpandCompleteParams>.Create("filebrowser/expandComplete");
    }

}