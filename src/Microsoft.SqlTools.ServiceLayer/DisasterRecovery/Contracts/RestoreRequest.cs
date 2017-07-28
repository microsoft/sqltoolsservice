//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Restore request parameters
    /// </summary>
    public class RestoreParams : GeneralRequestDetails
    {
        /// <summary>
        /// Restore session id. The parameter is optional and if passed, an existing plan will be used
        /// </summary>
        internal string SessionId
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.SessionId);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SessionId, value);
            }
        }

        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Comma delimited list of backup files
        /// </summary>
        internal string BackupFilePaths
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.BackupFilePaths);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.BackupFilePaths, value);
            }
        }

        /// <summary>
        /// Target Database name to restore to
        /// </summary>
        internal string TargetDatabaseName
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.TargetDatabaseName);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.TargetDatabaseName, value);
            }
        }

        /// <summary>
        /// Source Database name to restore from
        /// </summary>
        internal string SourceDatabaseName
        {
            get
            {
                return GetOptionValue<string>(RestoreOptionsHelper.SourceDatabaseName);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SourceDatabaseName, value);
            }
        }

        /// <summary>
        /// If set to true, the db files will be relocated to default data location in the server
        /// </summary>
        internal bool RelocateDbFiles
        {
            get
            {
                return GetOptionValue<bool>(RestoreOptionsHelper.RelocateDbFiles);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.RelocateDbFiles, value);
            }
        }

        /// <summary>
        /// Ids of the backup set to restore
        /// </summary>
        internal string[] SelectedBackupSets
        {
            get
            {
                return GetOptionValue<string[]>(RestoreOptionsHelper.SelectedBackupSets);
            }
            set
            {
                SetOptionValue(RestoreOptionsHelper.SelectedBackupSets, value);
            }
        }
    }

    /// <summary>
    /// Restore response
    /// </summary>
    public class RestoreResponse
    {
        /// <summary>
        /// Indicates if the restore task created successfully 
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// The task id assosiated witht the restore operation
        /// </summary>
        public string TaskId { get; set; }


        /// <summary>
        /// Errors occurred while creating the restore operation task
        /// </summary>
        public string ErrorMessage { get; set; }
    }

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
        public Dictionary<string, object> PlanDetails { get; set; }
    }

    public class RestoreRequest
    {
        public static readonly
            RequestType<RestoreParams, RestoreResponse> Type =
                RequestType<RestoreParams, RestoreResponse>.Create("disasterrecovery/restore");
    }

    public class RestorePlanRequest
    {
        public static readonly
            RequestType<RestoreParams, RestorePlanResponse> Type =
                RequestType<RestoreParams, RestorePlanResponse>.Create("disasterrecovery/restoreplan");
    }
}
