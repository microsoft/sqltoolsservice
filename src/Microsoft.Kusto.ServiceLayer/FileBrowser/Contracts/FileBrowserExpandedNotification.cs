//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Event params for expanding a node
    /// </summary>
    public class FileBrowserExpandedParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// Expand path
        /// </summary>
        public string ExpandPath;

        /// <summary>
        /// Children nodes
        /// </summary>
        public FileTreeNode[] Children;

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
    public class FileBrowserExpandedNotification
    {
        public static readonly
            EventType<FileBrowserExpandedParams> Type =
            EventType<FileBrowserExpandedParams>.Create("filebrowser/expandcomplete");
    }

}