//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters to cancel opening a file browser 
    /// </summary>
    public class FileBrowserCancelParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;
    }

    /// <summary>
    /// Request to cancel opening a file browser
    /// </summary>
    public class FileBrowserCancelRequest
    {
        public static readonly
            RequestType<FileBrowserCancelParams, bool> Type =
                RequestType<FileBrowserCancelParams, bool>.Create("filebrowser/cancel");
    }
}