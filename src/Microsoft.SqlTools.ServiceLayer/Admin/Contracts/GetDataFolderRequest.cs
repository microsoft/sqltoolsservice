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
    public class GetDataFolderParams
    {
        /// <summary>
        /// Uri identifier for the connection to get the server folder info for
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Get database info request mapping
    /// </summary>
    public class GetDataFolderRequest
    {
        public static readonly
            RequestType<GetDataFolderParams, string> Type =
                RequestType<GetDataFolderParams, string>.Create("admin/getdatafolder");
    }
}
