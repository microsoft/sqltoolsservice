//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.Kusto.ServiceLayer.FileBrowser
{
    /// <summary>
    /// Event arguments for validating selected files in file browser
    /// </summary>
    public sealed class FileBrowserValidateEventArgs : EventArgs
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Service which provide validation callback
        /// </summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// Selected files
        /// </summary>
        public string[] FilePaths { get; set; }
    }
}