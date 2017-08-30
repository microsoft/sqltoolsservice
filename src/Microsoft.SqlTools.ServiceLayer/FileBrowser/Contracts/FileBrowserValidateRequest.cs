//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Parameters for validating selected file paths
    /// </summary>
    public class FileBrowserValidateParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri;

        /// <summary>
        /// Type of service that uses the file browser
        /// </summary>
        public string ServiceType;

        /// <summary>
        /// Selected files
        /// </summary>
        public string[] SelectedFiles;
    }

    /// <summary>
    /// Response for validation
    /// </summary>
    public class FileBrowserValidateResponse
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
    /// Requst to validate the selected file paths
    /// </summary>
    class FileBrowserValidateRequest
    {
        public static readonly
            RequestType<FileBrowserValidateParams, FileBrowserValidateResponse> Type =
                RequestType<FileBrowserValidateParams, FileBrowserValidateResponse>.Create("filebrowser/validate");
    }
}
