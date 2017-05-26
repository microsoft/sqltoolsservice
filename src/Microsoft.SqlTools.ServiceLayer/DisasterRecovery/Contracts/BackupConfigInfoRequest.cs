//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{

    public class BackupConfigInfoResponse
    {
        public BackupConfigInfo BackupConfigInfo { get; set; }
    }

    public class BackupConfigInfoRequest
    {
        public static readonly
            RequestType<DefaultDatabaseInfoParams, BackupConfigInfoResponse> Type =
                RequestType<DefaultDatabaseInfoParams, BackupConfigInfoResponse>.Create("disasterrecovery/backupconfiginfo");
    }
}
