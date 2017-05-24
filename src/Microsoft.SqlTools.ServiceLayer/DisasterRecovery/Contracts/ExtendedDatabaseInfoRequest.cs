//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{

    public class ExtendedDatabaseInfoResponse
    {
        public ExtendedDatabaseInfo ExtendedDatabaseInfo { get; set; }
    }

    public class ExtendedDatabaseInfoRequest
    {
        public static readonly
            RequestType<DefaultDatabaseInfoParams, ExtendedDatabaseInfoResponse> Type =
                RequestType<DefaultDatabaseInfoParams, ExtendedDatabaseInfoResponse>.Create("disasterrecovery/extendeddatabaseinfo");
    }
}
