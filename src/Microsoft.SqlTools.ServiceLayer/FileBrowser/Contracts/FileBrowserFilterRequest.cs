//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts
{
    /// <summary>
    /// Request to change file filter
    /// </summary>
    public class FileBrowserFilterRequest
    {
        public static readonly
            RequestType<FileBrowserOpenParams, bool> Type =
                RequestType<FileBrowserOpenParams, bool>.Create("filebrowser/filter");
    }
}
