//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters for file browser operations
    /// </summary>
    public class FileBrowserParams
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
    public class FileBrowserResponse
    {
        /// <summary>
        /// Entire file/folder tree 
        /// </summary>
        public FileTree FileTree;

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
    /// Request to open a file browser
    /// </summary>
    public class FileBrowserOpenRequest
    {
        public static readonly
            RequestType<FileBrowserParams, FileBrowserResponse> Type =
                RequestType<FileBrowserParams, FileBrowserResponse>.Create("filebrowser/open");
    }

    /// <summary>
    /// Request to filter files with the specified extension
    /// </summary>
    public class FileBrowserFilterRequest
    {
        public static readonly
            RequestType<FileBrowserParams, FileBrowserResponse> Type =
                RequestType<FileBrowserParams, FileBrowserResponse>.Create("filebrowser/filterfiles");
    }
}
