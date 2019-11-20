//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Event params for opening a file browser
    /// Returns full directory structure on the server side
    /// </summary>
    public class FileBrowserOpenedParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// Entire file/folder tree 
        /// </summary>
        public FileTree FileTree;

        /// <summary>
        /// Result of the operation
        /// </summary>
        public bool Succeeded;

        /// <summary>
        /// Error message
        /// </summary>
        public string Message;
    }

    /// <summary>
    /// Notification for completing file browser opening
    /// </summary>
    public class FileBrowserOpenedNotification
    {
        public static readonly
            EventType<FileBrowserOpenedParams> Type =
            EventType<FileBrowserOpenedParams>.Create("filebrowser/opencomplete");
    }

}
