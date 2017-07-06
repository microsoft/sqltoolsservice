//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Includes method to all restore operations
    /// </summary>
    public class RestoreDatabaseService
    {
        /// <summary>
        /// Creates a restore plan, The result includes the information about the backup set, 
        /// the files and the database to restore to
        /// </summary>
        /// <param name="requestParam">Restore request</param>s
        /// <returns>Restore plan</returns>
        public RestorePlanResponse CreateRestorePlan(RestoreParams requestParam)
        {
            RestorePlanResponse response = new RestorePlanResponse()
            {
                DatabaseName = requestParam.DatabaseName
            };
            var restoreDataObject = CreateRestoreDataObject(requestParam);

            if (restoreDataObject != null)
            {
                response.DatabaseName = restoreDataObject.RestorePlanner.DatabaseName;
                response.DbFiles = restoreDataObject.DbFiles.Select(x => x.PhysicalName);
                response.CanRestore = CanRestore(restoreDataObject);
                
                if (!response.CanRestore)
                {
                    response.ErrorMessage = "Backup not supported.";
                }

                try
                {
                    restoreDataObject.CheckDbFilesLocation();
                }
                catch(Exception ex)
                {
                    response.RelocateFilesNeeded = true;
                }
                response.DefaultDataFolder = restoreDataObject.DefaultDataFileFolder;
                response.DefaultLogFolder = restoreDataObject.DefaultLogFileFolder;
            }
            else
            {
                response.ErrorMessage = "Failed to create restore plan";
                response.CanRestore = false;
            }

            return response;
        }

        /// <summary>
        /// Returns true if the restoring the restoreDataObject is supported in the service
        /// </summary>
        private bool CanRestore(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            if (restoreDataObject != null)
            {
                var backupTypes = restoreDataObject.GetBackupSetInfo();
                return backupTypes.Any(x => x.BackupType.StartsWith(RestoreConstants.TypeFull));
            }
            return false;
        }

        /// <summary>
        /// Create a restore data object that includes the plan to do the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        /// <returns></returns>
        private RestoreDatabaseTaskDataObject CreateRestoreDataObject(RestoreParams requestParam)
        {
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    requestParam.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                Server server = new Server(new ServerConnection(connInfo.ConnectionDetails.ServerName));
                RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(server, requestParam.DatabaseName);
                if (!string.IsNullOrEmpty(requestParam.BackupFilePath))
                {
                    restoreDataObject.AddFile(requestParam.BackupFilePath);
                }
                restoreDataObject.RestorePlanner.ReadHeaderFromMedia = !string.IsNullOrEmpty(requestParam.BackupFilePath);
                var dbNames = restoreDataObject.GetSourceDbNames();
                string dbName = dbNames.First();
                restoreDataObject.RestorePlanner.DatabaseName = dbName;
                restoreDataObject.UpdateRestorePlan(requestParam.RelocateDbFiles);

                return restoreDataObject;
            }
            return null;
        }

        /// <summary>
        /// Executes the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        public void ExecuteRestore(RestoreParams requestParam)
        {
            var restoreDataObject = CreateRestoreDataObject(requestParam);

            if (restoreDataObject != null && CanRestore(restoreDataObject))
            {
                restoreDataObject.RestorePlan.Execute();
            }
        }
    }
}
