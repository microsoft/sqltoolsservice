//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    /// <summary>
    /// Params for a get database info request
    /// </summar>
    public class GetDatabaseInfoParams
    {
        /// <summary>
        /// Uri identifier for the connection to get the database info for
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Response object for get database info
    /// </summary>
    public class GetDatabaseInfoResponse
    {
        /// <summary>
        /// The object containing the database info
        /// </summary>
        public DatabaseInfo DatabaseInfo { get; set; }
    }

    /// <summary>
    /// Get database info request mapping
    /// </summary>
    public class GetDatabaseInfoRequest
    {
        public static readonly
            RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse> Type =
                RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse>.Create("admin/getdatabaseinfo");
    }
}
