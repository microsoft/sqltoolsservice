//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Event params for opening a file browser
    /// Returns full directory structure on the server side
    /// </summary>
    public class FileBrowserOpenCompleteParams
    {
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
    public class FileBrowserOpenCompleteNotification
    {
        public static readonly
            EventType<FileBrowserOpenCompleteParams> Type =
            EventType<FileBrowserOpenCompleteParams>.Create("filebrowser/openComplete");
    }

}
