//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Includes method to all restore operations
    /// </summary>
    public class RestoreDatabaseHelper
    {

        /// <summary>
        /// Create a backup task for execution and cancellation
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        internal async Task<TaskResult> RestoreTaskAsync(SqlTask sqlTask)
        {
            sqlTask.AddMessage(SR.Task_InProgress, SqlTaskStatus.InProgress, true);
            RestoreDatabaseTaskDataObject restoreDataObject = sqlTask.TaskMetadata.Data as RestoreDatabaseTaskDataObject;
            TaskResult taskResult = null;

            if (restoreDataObject != null)
            {
                // Create a task to perform backup
                return await Task.Factory.StartNew(() =>
                {
                    TaskResult result = new TaskResult();
                    try
                    {
                        if (restoreDataObject.IsValid)
                        {
                            ExecuteRestore(restoreDataObject);
                            result.TaskStatus = SqlTaskStatus.Succeeded;
                        }
                        else
                        {
                            result.TaskStatus = SqlTaskStatus.Failed;
                            if (restoreDataObject.ActiveException != null)
                            {
                                result.ErrorMessage = restoreDataObject.ActiveException.Message;
                            }
                            else
                            {
                                result.ErrorMessage = SR.RestoreNotSupported;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                        if (ex.InnerException != null)
                        {
                            result.ErrorMessage += Environment.NewLine + ex.InnerException.Message;
                        }
                        if (restoreDataObject != null && restoreDataObject.ActiveException != null)
                        {
                            result.ErrorMessage += Environment.NewLine + restoreDataObject.ActiveException.Message;
                        }
                    }
                    return result;
                });
            }
            else
            {
                taskResult = new TaskResult();
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return taskResult;
        }

       

        /// <summary>
        /// Async task to cancel restore
        /// </summary>
        public async Task<TaskResult> CancelTaskAsync(SqlTask sqlTask)
        {
            RestoreDatabaseTaskDataObject restoreDataObject = sqlTask.TaskMetadata.Data as RestoreDatabaseTaskDataObject;
            TaskResult taskResult = null;


            if (restoreDataObject != null && restoreDataObject.IsValid)
            {
                // Create a task for backup cancellation request
                return await Task.Factory.StartNew(() =>
                {

                    foreach (Restore restore in restoreDataObject.RestorePlan.RestoreOperations)
                    {
                        restore.Abort();
                    }


                    return new TaskResult
                    {
                        TaskStatus = SqlTaskStatus.Canceled
                    };

                });
            }
            else
            {
                taskResult = new TaskResult();
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return taskResult;
        }

        /// <summary>
        /// Creates a restore plan, The result includes the information about the backup set, 
        /// the files and the database to restore to
        /// </summary>
        /// <param name="requestParam">Restore request</param>s
        /// <returns>Restore plan</returns>
        public RestorePlanResponse CreateRestorePlanResponse(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            RestorePlanResponse response = new RestorePlanResponse()
            {
                DatabaseName = restoreDataObject.RestoreParams.DatabaseName
            };
            try
            {
                if (restoreDataObject != null && restoreDataObject.IsValid)
                {
                    UpdateRestorePlan(restoreDataObject);

                    if (restoreDataObject != null && restoreDataObject.IsValid)
                    {
                        response.DatabaseName = restoreDataObject.RestorePlanner.DatabaseName;
                        response.DbFiles = restoreDataObject.DbFiles.Select(x => x.PhysicalName);
                        response.CanRestore = CanRestore(restoreDataObject);

                        if (!response.CanRestore)
                        {
                            response.ErrorMessage = SR.RestoreNotSupported;
                        }

                        response.RelocateFilesNeeded = !restoreDataObject.DbFilesLocationAreValid();
                        response.DefaultDataFolder = restoreDataObject.DefaultDataFileFolder;
                        response.DefaultLogFolder = restoreDataObject.DefaultLogFileFolder;
                    }
                    else
                    {
                        if (restoreDataObject.ActiveException != null)
                        {
                            response.ErrorMessage = restoreDataObject.ActiveException.Message;
                        }
                        else
                        {
                            response.ErrorMessage = SR.RestorePlanFailed;
                        }
                        response.CanRestore = false;
                    }
                }
                else
                {
                    response.ErrorMessage = SR.RestorePlanFailed;
                }
            }
            catch(Exception ex)
            {
                response.ErrorMessage = ex.Message;

                if (ex.InnerException != null)
                {
                    response.ErrorMessage += Environment.NewLine;
                    response.ErrorMessage += ex.InnerException.Message;
                }
            }
            return response;

        }

        /// <summary>
        /// Returns true if the restoring the restoreDataObject is supported in the service
        /// </summary>
        private static bool CanRestore(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            if (restoreDataObject != null)
            {
                var backupTypes = restoreDataObject.GetBackupSetInfo();
                return backupTypes.Any(x => x.BackupType.StartsWith(RestoreConstants.TypeFull));
            }
            return false;
        }

        /// <summary>
        /// Creates anew restore task object to do the restore operations
        /// </summary>
        /// <param name="restoreParams">Restore request parameters</param>
        /// <returns>Restore task object</returns>
        public RestoreDatabaseTaskDataObject CreateRestoreDatabaseTaskDataObject(RestoreParams restoreParams)
        {
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    restoreParams.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                Server server = new Server(new ServerConnection(connInfo.ConnectionDetails.ServerName));

                RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(server, restoreParams.DatabaseName);
                restoreDataObject.RestoreParams = restoreParams;
                return restoreDataObject;
            }
            return null;
        }

        /// <summary>
        /// Create a restore data object that includes the plan to do the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        /// <returns></returns>
        private void UpdateRestorePlan(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            if (!string.IsNullOrEmpty(restoreDataObject.RestoreParams.BackupFilePath))
            {
                restoreDataObject.AddFile(restoreDataObject.RestoreParams.BackupFilePath);
            }
            restoreDataObject.RestorePlanner.ReadHeaderFromMedia = !string.IsNullOrEmpty(restoreDataObject.RestoreParams.BackupFilePath);
            var dbNames = restoreDataObject.GetSourceDbNames();
            string dbName = dbNames.First();
            restoreDataObject.RestorePlanner.DatabaseName = dbName;
            restoreDataObject.UpdateRestorePlan(restoreDataObject.RestoreParams.RelocateDbFiles);
        }

        /// <summary>
        /// Executes the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        public void ExecuteRestore(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            UpdateRestorePlan(restoreDataObject);

            if (restoreDataObject != null && CanRestore(restoreDataObject))
            {
                restoreDataObject.RestorePlan.Execute();
            }
            else
            {
                throw new InvalidOperationException(SR.RestoreNotSupported);
            }
        }
    }
}
