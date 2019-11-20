//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts
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
    /// Request to expand a node in the file browser
    /// </summary>
    public class FileBrowserExpandRequest
    {
        public static readonly
            RequestType<FileBrowserExpandParams, bool> Type =
                RequestType<FileBrowserExpandParams, bool>.Create("filebrowser/expand");
    }
}