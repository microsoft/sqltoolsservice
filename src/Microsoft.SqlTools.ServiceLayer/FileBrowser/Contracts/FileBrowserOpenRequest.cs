//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters for opening file browser
    /// </summary>
    public class FileBrowserOpenParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// The initial path to expand the nodes for (e.g. Backup will set this path to default backup folder)
        /// </summary>
        public string ExpandPath;

        /// <summary>
        /// File extension filter (e.g. *.bak)
        /// </summary>
        public string[] FileFilters;
    }

    /// <summary>
    /// Response for opening/filtering a file browser
    /// Returns full directory structure on the server side
    /// </summary>
    public class FileBrowserOpenResponse
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
    /// Request to open a file browser
    /// </summary>
    public class FileBrowserOpenRequest
    {
        public static readonly
            RequestType<FileBrowserOpenParams, FileBrowserOpenResponse> Type =
                RequestType<FileBrowserOpenParams, FileBrowserOpenResponse>.Create("filebrowser/open");
    }
}
