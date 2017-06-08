//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts
{
    public class GetDatabaseInfoParams
    {
        public string OwnerUri { get; set; }
    }

    public class GetDatabaseInfoResponse
    {
        public DatabaseInfoWrapper Result { get; set; }
    }

    public class GetDatabaseInfoRequest
    {
        public static readonly
            RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse> Type =
                RequestType<GetDatabaseInfoParams, GetDatabaseInfoResponse>.Create("admin/getDatabaseInfo");
    }
}
