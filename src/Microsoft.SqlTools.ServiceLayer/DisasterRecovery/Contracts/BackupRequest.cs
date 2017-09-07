//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Backup parameters passed for execution and scripting
    /// </summary>
    public class BackupParams : IScriptableRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Backup metrics selected from the UI
        /// </summary>
        public BackupInfo BackupInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Response class for backup execution
    /// </summary>
    public class BackupResponse
    {
        public bool Result { get; set; }

        public int TaskId { get; set; }
    }

    /// <summary>
    /// Request class for backup execution
    /// </summary>
    public class BackupRequest
    {
        public static readonly
            RequestType<BackupParams, BackupResponse> Type =
                RequestType<BackupParams, BackupResponse>.Create("disasterrecovery/backup");
    }
}
