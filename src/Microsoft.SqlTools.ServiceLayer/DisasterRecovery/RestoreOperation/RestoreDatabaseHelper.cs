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
        public const string LastBackupTaken = "lastBackupTaken";
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
            sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
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
                            ExecuteRestore(restoreDataObject, sqlTask);
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
                DatabaseName = restoreDataObject.RestoreParams.TargetDatabaseName,
                PlanDetails = new System.Collections.Generic.Dictionary<string, object>()
            };
            try
            {
                if (restoreDataObject != null && restoreDataObject.IsValid)
                {
                    UpdateRestorePlan(restoreDataObject);

                    if (restoreDataObject != null && restoreDataObject.IsValid)
                    {
                        response.SessionId = restoreDataObject.SessionId;
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

                        response.PlanDetails.Add(LastBackupTaken, restoreDataObject.GetLastBackupTaken());

                        response.BackupSetsToRestore = restoreDataObject.GetSelectedBakupSets();
                        var dbNames = restoreDataObject.GetSourceDbNames();
                        response.DatabaseNamesFromBackupSets = dbNames == null ? new string[] { } : dbNames.ToArray();
                        
                        // Adding the default values for some of the options in the plan details 
                        bool isTailLogBackupPossible = restoreDataObject.IsTailLogBackupPossible(restoreDataObject.RestorePlanner.DatabaseName);
                        // Default backup tail-log. It's true when tail-log backup is possible for the source database
                        response.PlanDetails.Add(RestoreOptionsHelper.DefaultBackupTailLog, isTailLogBackupPossible);
                        // Default backup file for tail-log bacup when  Tail-Log bachup is set to true
                        response.PlanDetails.Add(RestoreOptionsHelper.DefaultTailLogBackupFile, 
                            restoreDataObject.Util.GetDefaultTailLogbackupFile(restoreDataObject.RestorePlan.DatabaseName));
                        // Default stand by file path for when RESTORE WITH STANDBY is selected
                        response.PlanDetails.Add(RestoreOptionsHelper.DefaultStandbyFile, restoreDataObject.Util.GetDefaultStandbyFile(restoreDataObject.RestorePlan.DatabaseName));
                        // Default Data folder path in the target server
                        response.PlanDetails.Add(RestoreOptionsHelper.DefaultDataFileFolder, restoreDataObject.DefaultDataFileFolder);
                        // Default log folder path in the target server
                        response.PlanDetails.Add(RestoreOptionsHelper.DefaultLogFileFolder, restoreDataObject.DefaultLogFileFolder);
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
            return restoreDataObject != null && restoreDataObject.RestorePlan != null && restoreDataObject.RestorePlan.RestoreOperations != null
                && restoreDataObject.RestorePlan.RestoreOperations.Count > 0;
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
            else
            {
                restoreTaskObject.RestoreParams = restoreParams;
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

            restoreDataObject.RestoreOptions.KeepReplication = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.KeepReplication);
            restoreDataObject.RestoreOptions.ReplaceDatabase = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.ReplaceDatabase);
            restoreDataObject.RestoreOptions.SetRestrictedUser = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.SetRestrictedUser);
            string recoveryState = restoreDataObject.RestoreParams.GetOptionValue<string>(RestoreOptionsHelper.RecoveryState);
            object databaseRecoveryState;
            if (Enum.TryParse(typeof(DatabaseRecoveryState), recoveryState, out databaseRecoveryState))
            {
                restoreDataObject.RestoreOptions.RecoveryState = (DatabaseRecoveryState)databaseRecoveryState;
            }
            bool isTailLogBackupPossible = restoreDataObject.IsTailLogBackupPossible(restoreDataObject.RestorePlanner.DatabaseName);
            if (isTailLogBackupPossible)
            {
                restoreDataObject.RestorePlanner.BackupTailLog = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.BackupTailLog);
                restoreDataObject.TailLogBackupFile = restoreDataObject.RestoreParams.GetOptionValue<string>(RestoreOptionsHelper.TailLogBackupFile);
                restoreDataObject.TailLogWithNoRecovery = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.TailLogWithNoRecovery);
            }
            else
            {
                restoreDataObject.RestorePlanner.BackupTailLog = false;
            }

            restoreDataObject.CloseExistingConnections = restoreDataObject.RestoreParams.GetOptionValue<bool>(RestoreOptionsHelper.CloseExistingConnections);

            restoreDataObject.UpdateRestorePlan(restoreDataObject.RestoreParams.RelocateDbFiles);
        }

        /// <summary>
        /// Executes the restore operation
        /// </summary>
        /// <param name="requestParam"></param>
        public void ExecuteRestore(RestoreDatabaseTaskDataObject restoreDataObject, SqlTask sqlTask = null)
        {
            // Restore Plan should be already created and updated at this point
            UpdateRestorePlan(restoreDataObject);

            if (restoreDataObject != null && CanRestore(restoreDataObject))
            {
                try
                {
                    restoreDataObject.SqlTask = sqlTask;
                    restoreDataObject.Execute();
                    RestoreDatabaseTaskDataObject cachedRestoreDataObject;
                    this.restoreSessions.TryRemove(restoreDataObject.SessionId, out cachedRestoreDataObject);
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
