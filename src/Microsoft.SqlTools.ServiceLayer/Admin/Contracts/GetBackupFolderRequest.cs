//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    /// <summary>
    /// Params for a get database info request
    /// </summar>
    public class GetBackupFolderParams
    {
        /// <summary>
        /// URI identifier for the connection to get the server backup folder info for
        /// </summary>
        public string ConnectionUri { get; set; }
    }

    /// <summary>
    /// Get database info request mapping
    /// </summary>
    public class GetBackupFolderRequest
    {
        public static readonly
            RequestType<GetBackupFolderParams, string> Type =
                RequestType<GetBackupFolderParams, string>.Create("admin/getbackupfolder");
    }
}