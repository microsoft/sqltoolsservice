//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{
    /// <summary>
    /// Restore request parameters
    /// </summary>
    public class RestoreParams
    {
        /// <summary>
        /// The Uri to find the connection to do the restore operations
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// The backup file path
        /// </summary>
        public string BackupFilePath { get; set; }

        /// <summary>
        /// Target Database name to restore to
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// If set to true, the db files will be relocated to default data location in the server
        /// </summary>
        public bool RelocateDbFiles { get; set; }
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
    /// Restore Plan Response
    /// </summary>
    public class RestorePlanResponse
    {
        /// <summary>
        /// The backup file path
        /// </summary>
        public string BackupFilePath { get; set; }

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
        public IEnumerable<string> DbFiles { get; set; }

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
