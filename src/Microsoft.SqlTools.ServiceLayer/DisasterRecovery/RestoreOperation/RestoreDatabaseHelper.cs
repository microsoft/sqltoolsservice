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
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    /// <summary>
    /// Includes method to all restore operations
    /// </summary>
    public class RestoreDatabaseHelper
    {
        public const string LastBackupTaken = "lastBackupTaken";
        private ConcurrentDictionary<string, RestoreDatabaseTaskDataObject> sessions = new ConcurrentDictionary<string, RestoreDatabaseTaskDataObject>(); 

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
        /// Creates response which includes information about the server given to restore (default data location, db names with backupsets)
        /// </summary>
        public RestoreConfigInfoResponse CreateConfigInfoResponse(RestoreConfigInfoRequestParams restoreConfigInfoRequest)
        {
            RestoreConfigInfoResponse response = new RestoreConfigInfoResponse();
            RestoreDatabaseTaskDataObject restoreTaskObject = CreateRestoreForNewSession(restoreConfigInfoRequest.OwnerUri);
            if (restoreTaskObject != null)
            {
                // Default Data folder path in the target server
                response.ConfigInfo.Add(RestoreOptionsHelper.DataFileFolder, restoreTaskObject.DefaultDataFileFolder);
                // Default log folder path in the target server
                response.ConfigInfo.Add(RestoreOptionsHelper.LogFileFolder, restoreTaskObject.DefaultLogFileFolder);
                // The db names with backup set
                response.ConfigInfo.Add(RestoreOptionsHelper.SourceDatabaseNamesWithBackupSets, restoreTaskObject.GetDatabaseNamesWithBackupSets());
            }

            return response;
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
                PlanDetails = new System.Collections.Generic.Dictionary<string, RestorePlanDetailInfo>()
            };
            try
            {
                if (restoreDataObject != null && restoreDataObject.IsValid)
                {
                    UpdateRestorePlan(restoreDataObject);

                    if (restoreDataObject != null && restoreDataObject.IsValid)
                    {
                        response.SessionId = restoreDataObject.SessionId;
                        response.DatabaseName = restoreDataObject.TargetDatabaseName;

                        response.PlanDetails.Add(RestoreOptionsHelper.TargetDatabaseName, 
                            RestoreOptionFactory.Instance.CreateAndValidate(RestoreOptionsHelper.TargetDatabaseName, restoreDataObject));
                        response.PlanDetails.Add(RestoreOptionsHelper.SourceDatabaseName, 
                            RestoreOptionFactory.Instance.CreateAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDataObject));

                        response.PlanDetails.Add(RestoreOptionsHelper.ReadHeaderFromMedia, RestorePlanDetailInfo.Create(
                           name: RestoreOptionsHelper.ReadHeaderFromMedia,
                           currentValue: restoreDataObject.RestorePlanner.ReadHeaderFromMedia));
                        response.DbFiles = restoreDataObject.DbFiles.Select(x => new RestoreDatabaseFileInfo
                        {
                            FileType = x.DbFileType,
                            LogicalFileName = x.LogicalName,
                            OriginalFileName = x.PhysicalName,
                            RestoreAsFileName = x.PhysicalNameRelocate
                        });
                        response.CanRestore = CanRestore(restoreDataObject);

                        response.PlanDetails.Add(LastBackupTaken, 
                            RestorePlanDetailInfo.Create(name: LastBackupTaken, currentValue: restoreDataObject.GetLastBackupTaken(), isReadOnly: true));

                        response.BackupSetsToRestore = restoreDataObject.GetSelectedBakupSets();
                        var dbNames = restoreDataObject.SourceDbNames;
                        response.DatabaseNamesFromBackupSets = dbNames == null ? new string[] { } : dbNames.ToArray();

                        RestoreOptionsHelper.AddOptions(response, restoreDataObject);
                      
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
            string sessionId = string.IsNullOrWhiteSpace(restoreParams.SessionId) ? Guid.NewGuid().ToString() : restoreParams.SessionId;
            if (!sessions.TryGetValue(sessionId, out restoreTaskObject))
            {
                restoreTaskObject = CreateRestoreForNewSession(restoreParams.OwnerUri, restoreParams.TargetDatabaseName);
                sessions.AddOrUpdate(sessionId, restoreTaskObject, (key, old) => restoreTaskObject);
            }
            restoreTaskObject.SessionId = sessionId;
            restoreTaskObject.RestoreParams = restoreParams;
            
            return restoreTaskObject;
        }

        private RestoreDatabaseTaskDataObject CreateRestoreForNewSession(string ownerUri, string targetDatabaseName = null)
        {
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    ownerUri,
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

                RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(server, targetDatabaseName);
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
            bool shouldCreateNewPlan = restoreDataObject.ShouldCreateNewPlan();
            
            if (!string.IsNullOrEmpty(restoreDataObject.RestoreParams.BackupFilePaths))
            {
                restoreDataObject.AddFiles(restoreDataObject.RestoreParams.BackupFilePaths);
            }
            restoreDataObject.RestorePlanner.ReadHeaderFromMedia = restoreDataObject.RestoreParams.ReadHeaderFromMedia;

            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.SourceDatabaseName, restoreDataObject);
            RestoreOptionFactory.Instance.SetAndValidate(RestoreOptionsHelper.TargetDatabaseName, restoreDataObject);

            if (shouldCreateNewPlan)
            {
                restoreDataObject.CreateNewRestorePlan();
                restoreDataObject.RestoreParams.SelectedBackupSets = null;
            }

            restoreDataObject.UpdateRestorePlan();

        }

        private bool CanChangeTargetDatabase(RestoreDatabaseTaskDataObject restoreDataObject)
        {
            return DatabaseUtils.IsSystemDatabaseConnection(restoreDataObject.Server.ConnectionContext.DatabaseName);
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
