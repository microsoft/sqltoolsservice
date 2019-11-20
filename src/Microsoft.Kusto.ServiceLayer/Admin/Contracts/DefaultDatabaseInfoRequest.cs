//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Admin.Contracts
{

    public class DefaultDatabaseInfoParams
    {
        public string OwnerUri { get; set; }
    }

    public class DefaultDatabaseInfoResponse
    {
        public DatabaseInfo DefaultDatabaseInfo { get; set; }
    }


    public class DefaultDatabaseInfoRequest
    {
        public static readonly
            RequestType<DefaultDatabaseInfoParams, DefaultDatabaseInfoResponse> Type =
                RequestType<DefaultDatabaseInfoParams, DefaultDatabaseInfoResponse>.Create("admin/defaultdatabaseinfo");
    }
}
