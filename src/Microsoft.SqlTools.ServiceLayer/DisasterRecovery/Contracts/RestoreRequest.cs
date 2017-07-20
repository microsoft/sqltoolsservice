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
        public string SessionId
        {
            get
            {
                return GetOptionValue<string>("sessionId");
            }
            set
            {
                SetOptionValue("sessionId", value);
            }
        }

        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Comma delimited list of backup files
        /// </summary>
        public string BackupFilePaths
        {
            get
            {
                return GetOptionValue<string>("backupFilePaths");
            }
            set
            {
                SetOptionValue("backupFilePaths", value);
            }
        }

        /// <summary>
        /// Target Database name to restore to
        /// </summary>
        public string TargetDatabaseName
        {
            get
            {
                return GetOptionValue<string>("targetDatabaseName");
            }
            set
            {
                SetOptionValue("targetDatabaseName", value);
            }
        }

        /// <summary>
        /// Source Database name to restore from
        /// </summary>
        public string SourceDatabaseName
        {
            get
            {
                return GetOptionValue<string>("sourceDatabaseName");
            }
            set
            {
                SetOptionValue("sourceDatabaseName", value);
            }
        }

        /// <summary>
        /// If set to true, the db files will be relocated to default data location in the server
        /// </summary>
        public bool RelocateDbFiles
        {
            get
            {
                return GetOptionValue<bool>("relocateDbFiles");
            }
            set
            {
                SetOptionValue("relocateDbFiles", value);
            }
        }

        /// <summary>
        /// Ids of the backup set to restore
        /// </summary>
        public string[] SelectedBackupSets
        {
            get
            {
                return GetOptionValue<string[]>("selectedBackupSets");
            }
            set
            {
                SetOptionValue("selectedBackupSets", value);
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
        public string RestoreSessionId { get; set; }

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
        /// Server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Database name to restore to
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Indicates whether relocating the db files is required 
        /// because the original file paths are not valid in the target server
        /// </summary>
        public bool RelocateFilesNeeded { get; set; }

        /// <summary>
        /// Default Data folder path in the target server
        /// </summary>
        public string DefaultDataFolder { get; set; }

        /// <summary>
        /// Default log folder path in the target server
        /// </summary>
        public string DefaultLogFolder { get; set; }
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
