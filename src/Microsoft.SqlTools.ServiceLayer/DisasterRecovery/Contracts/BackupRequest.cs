//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    public class BackupParams
    {
        public string OwnerUri { get; set; }

        public BackupInfo BackupInfo { get; set; }

        public bool IsScripting { get; set; }
    }

    public class BackupResponse
    {
        public bool Result { get; set; }

        public int TaskId { get; set; }
    }

    public class BackupRequest
    {
        public static readonly
            RequestType<BackupParams, BackupResponse> Type =
                RequestType<BackupParams, BackupResponse>.Create("disasterrecovery/backup");
    }
}
