//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    public class RestoreParams
    {
        public string OwnerUri { get; set; }

        public string BackupFilePath { get; set; }

        public string DatabaseName { get; set; }

        public bool RelocateDbFiles { get; set; }
    }

    public class RestoreResponse
    {
        public bool Result { get; set; }

        public int TaskId { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class RestorePlanResponse
    {
        public string BackupFilePath { get; set; }

        public bool CanRestore { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<string> DbFiles { get; set; }

        public string ServerName { get; set; }

        public string DatabaseName { get; set; }

        public bool RelocateFilesNeeded { get; set; }

        public string DefaultDataFolder { get; set; }

        public string DefaultLogFolder { get; set; }
    }

    public class RestoreRequest
    {
        public static readonly
            RequestType<RestoreParams, RestoreResponse> Type =
                RequestType<RestoreParams, RestoreResponse>.Create("disasterrecovery/restore");
    }

    public class RestoreDbFilesRequest
    {
        public static readonly
            RequestType<RestoreParams, RestorePlanResponse> Type =
                RequestType<RestoreParams, RestorePlanResponse>.Create("disasterrecovery/restoreDbFiles");
    }
}
