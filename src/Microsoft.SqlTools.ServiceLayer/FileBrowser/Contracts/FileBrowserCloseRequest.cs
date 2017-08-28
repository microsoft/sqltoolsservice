//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters to pass to close browser
    /// </summary>
    public class FileBrowserCloseParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// Type of service that uses the file browser
        /// </summary>
        public ServiceTypes ServiceType;

        /// <summary>
        /// Selected files
        /// </summary>
        public string[] selectedFiles;
    }

    /// <summary>
    /// Response for closing browser
    /// </summary>
    public class FileBrowserCloseResponse
    {
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
    /// Requst to close the file browser 
    /// </summary>
    class FileBrowserCloseRequest
    {
        public static readonly
            RequestType<FileBrowserCloseParams, FileBrowserCloseResponse> Type =
                RequestType<FileBrowserCloseParams, FileBrowserCloseResponse>.Create("filebrowser/close");
    }
}
