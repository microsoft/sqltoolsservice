//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Database file info
    /// </summary>
    public class RestoreDatabaseFileInfo
    {
        /// <summary>
        /// File type (Rows Data, Log ...)
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Logical Name
        /// </summary>
        public string LogicalFileName { get; set; }

        /// <summary>
        /// Original location of the file to restore to
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// The file to restore to
        /// </summary>
        public string RestoreAsFileName { get; set; }
    }

    /// <summary>
    /// Restore Plan Response
    /// </summary>
    public class RestorePlanResponse
    {
        /// <summary>
        /// Restore session id, can be used in restore request to use an existing restore plan
        /// </summary>
        public string SessionId { get; set; }


        /// <summary>
        /// The list of backup sets to restore
        /// </summary>
        public DatabaseFileInfo[] BackupSetsToRestore { get; set; }

        /// <summary>
        /// Indicates whether the restore operation is supported 
        /// </summary>
        public bool CanRestore { get; set; }

        /// <summary>
        /// Errors occurred while creating restore plan
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The db files included in the backup file
        /// </summary>
        public IEnumerable<RestoreDatabaseFileInfo> DbFiles { get; set; }

        /// <summary>
        /// Database names extracted from backup sets
        /// </summary>
        public string[] DatabaseNamesFromBackupSets { get; set; }

        /// <summary>
        /// For testing purpose to verify the target database
        /// </summary>
        internal string DatabaseName { get; set; }

        /// <summary>
        /// Plan details
        /// </summary>
        public Dictionary<string, RestorePlanDetailInfo> PlanDetails { get; set; }
    }

    public class RestorePlanRequest
    {
        public static readonly
            RequestType<RestoreParams, RestorePlanResponse> Type =
                RequestType<RestoreParams, RestorePlanResponse>.Create("restore/restoreplan");
    }

    public class CancelRestorePlanRequest
    {
        public static readonly
            RequestType<RestoreParams, bool> Type =
                RequestType<RestoreParams, bool>.Create("restore/cancelrestoreplan");
    }
}
