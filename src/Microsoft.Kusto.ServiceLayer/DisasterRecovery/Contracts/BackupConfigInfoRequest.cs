//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Admin.Contracts;

namespace Microsoft.Kusto.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Response class which returns backup configuration information
    /// </summary>
    public class BackupConfigInfoResponse
    {
        public BackupConfigInfo BackupConfigInfo { get; set; }
    }

    /// <summary>
    /// Request class to get backup configuration information
    /// </summary>
    public class BackupConfigInfoRequest
    {
        public static readonly
            RequestType<DefaultDatabaseInfoParams, BackupConfigInfoResponse> Type =
                RequestType<DefaultDatabaseInfoParams, BackupConfigInfoResponse>.Create("backup/backupconfiginfo");
    }
}
