//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters to pass to close file browser
    /// </summary>
    public class FileBrowserCloseParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;
    }

    /// <summary>
    /// Response for closing the browser
    /// </summary>
    public class FileBrowserCloseResponse
    {
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
    /// Requst to close the file browser
    /// </summary>
    class FileBrowserCloseRequest
    {
        public static readonly
            RequestType<FileBrowserCloseParams, FileBrowserCloseResponse> Type =
                RequestType<FileBrowserCloseParams, FileBrowserCloseResponse>.Create("filebrowser/close");
    }

    /// <summary>
    /// Notification for close completion
    /// </summary>
    public class FileBrowserClosedNotification
    {
        public static readonly
            EventType<FileBrowserCloseResponse> Type =
            EventType<FileBrowserCloseResponse>.Create("filebrowser/closecomplete");
    }
}
