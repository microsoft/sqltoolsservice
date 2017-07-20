//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Linq;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System.Collections.Concurrent;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Includes method to all restore operations
    /// </summary>
    public class RestoreDatabaseHelper
    {

        private static RestoreDatabaseHelper instance = new RestoreDatabaseHelper();
        private ConcurrentDictionary<string, RestoreDatabaseTaskDataObject> restoreSessions = new ConcurrentDictionary<string, RestoreDatabaseTaskDataObject>();

        internal RestoreDatabaseHelper()
        {

        }

        public static RestoreDatabaseHelper Instance
        {
            get
            {
                return instance;
            }
        }

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
                DatabaseName = restoreDataObject.RestoreParams.TargetDatabaseName
            };
            try
            {
                if (restoreDataObject != null && restoreDataObject.IsValid)
                {
                    UpdateRestorePlan(restoreDataObject);

                    if (restoreDataObject != null && restoreDataObject.IsValid)
                    {
                        response.RestoreSessionId = restoreDataObject.SessionId;
                        response.DatabaseName = restoreDataObject.TargetDatabase;
                        response.DbFiles = restoreDataObject.DbFiles.Select(x => new RestoreDatabaseFileInfo
                        {
                            FileType = x.DbFileType,
                            LogicalFileName = x.LogicalName,
                            OriginalFileName = x.PhysicalName,
                            RestoreAsFileName = x.PhysicalNameRelocate
                        });
                        response.CanRestore = CanRestore(restoreDataObject);

                        if (!response.CanRestore)
                        {
                            response.ErrorMessage = SR.RestoreNotSupported;
                        }

                        response.BackupSetsToRestore = restoreDataObject.GetBackupSetInfo().Select(x => new DatabaseFileInfo(x.ConvertPropertiesToArray())).ToArray();
                        var dbNames = restoreDataObject.GetSourceDbNames();
                        response.DatabaseNamesFromBackupSets = dbNames == null ? new string[] { } : dbNames.ToArray();
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
                var selectedBackupSets = restoreDataObject.RestoreParams.SelectedBackupSets;
                return backupTypes.Any(x => (selectedBackupSets == null || selectedBackupSets.Contains(x.GetPropertyValueAsString(DatabaseFileInfo.IdPropertyName))) 
                && x.BackupType.StartsWith(RestoreConstants.TypeFull));
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
            RestoreDatabaseTaskDataObject restoreTaskObject = null;
            if (!string.IsNullOrWhiteSpace(restoreParams.SessionId))
            {
                this.restoreSessions.TryGetValue(restoreParams.SessionId, out restoreTaskObject);
            }

            if (restoreTaskObject == null)
            {
                restoreTaskObject = CreateRestoreForNewSession(restoreParams);
                string sessionId = string.IsNullOrWhiteSpace(restoreParams.SessionId) ? Guid.NewGuid().ToString() : restoreParams.SessionId;
                this.restoreSessions.AddOrUpdate(sessionId, restoreTaskObject, (key, oldSession) => restoreTaskObject);
                restoreTaskObject.SessionId = sessionId;
            }
            return restoreTaskObject;
        }

        private RestoreDatabaseTaskDataObject CreateRestoreForNewSession(RestoreParams restoreParams)
        {
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    restoreParams.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                SqlConnection connection;
                DbConnection dbConnection = connInfo.AllConnections.First();
                ReliableSqlConnection reliableSqlConnection = dbConnection as ReliableSqlConnection;
                SqlConnection sqlConnection = dbConnection as SqlConnection;
                if (reliableSqlConnection != null)
                {
                    connection = reliableSqlConnection.GetUnderlyingConnection();
                }
                else if (sqlConnection != null)
                {
                    connection = sqlConnection;
                }
                else
                {
                    Logger.Write(LogLevel.Warning, "Cannot find any sql connection for restore operation");
                    return null;
                }
                Server server = new Server(new ServerConnection(connection));

                RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(server, restoreParams.TargetDatabaseName);
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
            if (!string.IsNullOrEmpty(restoreDataObject.RestoreParams.BackupFilePaths))
            {
                restoreDataObject.AddFiles(restoreDataObject.RestoreParams.BackupFilePaths);
            }
            restoreDataObject.RestorePlanner.ReadHeaderFromMedia = !string.IsNullOrEmpty(restoreDataObject.RestoreParams.BackupFilePaths);

            if (string.IsNullOrWhiteSpace(restoreDataObject.RestoreParams.SourceDatabaseName))
            {
                restoreDataObject.RestorePlanner.DatabaseName = restoreDataObject.DefaultDbName;
            }
            else
            {
                restoreDataObject.RestorePlanner.DatabaseName = restoreDataObject.RestoreParams.SourceDatabaseName;
            }
            restoreDataObject.TargetDatabase = restoreDataObject.RestoreParams.TargetDatabaseName;
            //TODO: used for other types of restore
            /*bool isTailLogBackupPossible = restoreDataObject.RestorePlanner.IsTailLogBackupPossible(restoreDataObject.RestorePlanner.DatabaseName);
            restoreDataObject.RestorePlanner.BackupTailLog = isTailLogBackupPossible;
            restoreDataObject.TailLogBackupFile = restoreDataObject.Util.GetDefaultTailLogbackupFile(dbName);
            restoreDataObject.RestorePlanner.TailLogBackupFile = restoreDataObject.TailLogBackupFile;
            */

            restoreDataObject.UpdateRestorePlan(restoreDataObject.RestoreParams.RelocateDbFiles);
        }

        /// <summary>
        /// Executes the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        public void ExecuteRestore(RestoreDatabaseTaskDataObject restoreDataObject, SqlTask sqlTask = null)
        {
            // Restore Plan should be already created and updated at this point
            //UpdateRestorePlan(restoreDataObject);

            if (restoreDataObject != null && CanRestore(restoreDataObject))
            {
                try
                {
                    restoreDataObject.SqlTask = sqlTask;
                    restoreDataObject.Execute();
                }
                catch(Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new InvalidOperationException(SR.RestoreNotSupported);
            }
        }
    }
}
