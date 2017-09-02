//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class FileBrowserValidateEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ServiceType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] FilePaths { get; set; }
    }
}