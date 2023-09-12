﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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
        /// File extension filter (e.g. *.bak). Ignored if <see cref="ShowFoldersOnly"/> is set to <c>true</c>.
        /// </summary>
        public string[] FileFilters;

        /// <summary>
        /// True if this is a request to change file filter
        /// </summary>
        public bool ChangeFilter;

        /// <summary>
        /// Whether to only show folders in the file browser.
        /// </summary>
        public bool? ShowFoldersOnly;
    }

    /// <summary>
    /// Request to open a file browser
    /// </summary>
    public class FileBrowserOpenRequest
    {
        public static readonly
            RequestType<FileBrowserOpenParams, bool> Type =
                RequestType<FileBrowserOpenParams, bool>.Create("filebrowser/open");
    }
}
