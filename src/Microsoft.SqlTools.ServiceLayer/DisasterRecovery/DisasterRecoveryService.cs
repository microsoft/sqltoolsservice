//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Service for Backup and Restore
    /// </summary>
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());
        private static ConnectionService connectionService = null;
        private RestoreDatabaseHelper restoreDatabaseService = RestoreDatabaseHelper.Instance;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DisasterRecoveryService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static DisasterRecoveryService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }
            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            // Get database info
            serviceHost.SetRequestHandler(BackupConfigInfoRequest.Type, HandleBackupConfigInfoRequest);
            // Create backup
            serviceHost.SetRequestHandler(BackupRequest.Type, HandleBackupRequest);

            // Create respore task
            serviceHost.SetRequestHandler(RestoreRequest.Type, HandleRestoreRequest);
            // Create respore plan
            serviceHost.SetRequestHandler(RestorePlanRequest.Type, HandleRestorePlanRequest);
        }

        /// <summary>
        /// Handle request to get backup configuration info
        /// </summary>
        /// <param name="optionsParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task HandleBackupConfigInfoRequest(
            DefaultDatabaseInfoParams optionsParams,
            RequestContext<BackupConfigInfoResponse> requestContext)
        {
            try
            {
                var response = new BackupConfigInfoResponse();
                ConnectionInfo connInfo;
                DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                        optionsParams.OwnerUri,
                        out connInfo);

                if (connInfo != null)
                {
                    DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true);
                    SqlConnection sqlConn = GetSqlConnection(connInfo);
                    if ((sqlConn != null) && !connInfo.IsSqlDW && !connInfo.IsAzure)
                    {
                        BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
                        backupConfigInfo.DatabaseInfo = AdminService.GetDatabaseInfo(connInfo);
                        response.BackupConfigInfo = backupConfigInfo;
                    }
                }

                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handles a restore request
        /// </summary>
        internal async Task HandleRestorePlanRequest(
            RestoreParams restoreParams,
            RequestContext<RestorePlanResponse> requestContext)
        {
            RestorePlanResponse response = new RestorePlanResponse();

            try
            {
                ConnectionInfo connInfo;
                bool supported = IsBackupRestoreOperationSupported(restoreParams, out connInfo);

                if (supported && connInfo != null)
                {
                    RestoreDatabaseTaskDataObject restoreDataObject = this.restoreDatabaseService.CreateRestoreDatabaseTaskDataObject(restoreParams);
                    response = this.restoreDatabaseService.CreateRestorePlanResponse(restoreDataObject);
                }
                else
                {
                    response.CanRestore = false;
                    response.ErrorMessage = SR.RestoreNotSupported;
                }
                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                response.CanRestore = false;
                response.ErrorMessage = ex.Message;
                await requestContext.SendResult(response);
            }
        }

        /// <summary>
        /// Handles a restore request
        /// </summary>
        internal async Task HandleRestoreRequest(
            RestoreParams restoreParams,
            RequestContext<RestoreResponse> requestContext)
        {
            RestoreResponse response = new RestoreResponse();

            try
            {
                ConnectionInfo connInfo;
                bool supported = IsBackupRestoreOperationSupported(restoreParams, out connInfo);

                if (supported && connInfo != null)
                {
                    try
                    {
                        RestoreDatabaseTaskDataObject restoreDataObject = this.restoreDatabaseService.CreateRestoreDatabaseTaskDataObject(restoreParams);

                        if (restoreDataObject != null)
                        {
                            // create task metadata
                            TaskMetadata metadata = new TaskMetadata();
                            metadata.ServerName = connInfo.ConnectionDetails.ServerName;
                            metadata.DatabaseName = connInfo.ConnectionDetails.DatabaseName;
                            metadata.Name = SR.RestoreTaskName;
                            metadata.IsCancelable = true;
                            metadata.Data = restoreDataObject;

                            // create restore task and perform
                            SqlTask sqlTask = SqlTaskManager.Instance.CreateAndRun(metadata, this.restoreDatabaseService.RestoreTaskAsync, restoreDatabaseService.CancelTaskAsync);
                            response.TaskId = sqlTask.TaskId.ToString();
                        }
                        else
                        {
                            response.ErrorMessage = SR.RestorePlanFailed;
                        }
                    }
                    catch (Exception ex)
                    {
                        response.ErrorMessage = ex.Message;
                    }
                }
                else
                {
                    response.ErrorMessage = SR.RestoreNotSupported;
                }

                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
                response.Result = false;
                response.ErrorMessage = ex.Message;
                await requestContext.SendResult(response);
            }
        }

        /// <summary>
        /// Handles a backup request
        /// </summary>
        internal static async Task HandleBackupRequest(
            BackupParams backupParams,
            RequestContext<BackupResponse> requestContext)
        {     
            try
            {       
                ConnectionInfo connInfo;
                DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                        backupParams.OwnerUri,
                        out connInfo);

                if (connInfo != null)
                {
                    DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true);
                    SqlConnection sqlConn = GetSqlConnection(connInfo);
                    if ((sqlConn != null) && !connInfo.IsSqlDW && !connInfo.IsAzure)
                    {
                        BackupOperation backupOperation = DisasterRecoveryService.Instance.SetBackupInput(helper.DataContainer, sqlConn, backupParams.BackupInfo);
                        SqlTask sqlTask = null;

                        // create task metadata
                        TaskMetadata metadata = new TaskMetadata();
                        metadata.ServerName = connInfo.ConnectionDetails.ServerName;
                        metadata.DatabaseName = connInfo.ConnectionDetails.DatabaseName;
                        metadata.Data = backupOperation;
                        metadata.IsCancelable = true;

                        if (backupParams.IsScripting)
                        {
                            metadata.Name = string.Format("{0} {1}", SR.BackupTaskName, SR.ScriptTaskName);
                            metadata.TaskExecutionMode = TaskExecutionMode.Script;
                            sqlTask = SqlTaskManager.Instance.CreateTask(metadata, Instance.BackupScriptTaskAsync);
                            sqlTask.Run();
                        }
                        else
                        {
                            metadata.Name = SR.BackupTaskName;
                            metadata.TaskExecutionMode = TaskExecutionMode.Execute;
                            sqlTask = SqlTaskManager.Instance.CreateAndRun(metadata, Instance.PerformBackupTaskAsync, Instance.CancelBackupTaskAsync);
                        }
                    }
                }

                await requestContext.SendResult(new BackupResponse());
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal static SqlConnection GetSqlConnection(ConnectionInfo connInfo)
        {
            try
            {
                // increase the connection timeout to at least 30 seconds and and build connection string
                // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                connInfo.ConnectionDetails.PersistSecurityInfo = true;
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;

                // open a dedicated binding server connection
                SqlConnection sqlConn = new SqlConnection(connectionString);
                sqlConn.Open();
                return sqlConn;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private bool IsBackupRestoreOperationSupported(RestoreParams restoreParams, out ConnectionInfo connectionInfo)
        {
            SqlConnection sqlConn = null;
            try
            {
                ConnectionInfo connInfo;
                DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                        restoreParams.OwnerUri,
                        out connInfo);

                if (connInfo != null)
                {
                    sqlConn = GetSqlConnection(connInfo);
                    if ((sqlConn != null) && !connInfo.IsSqlDW && !connInfo.IsAzure)
                    {
                        connectionInfo = connInfo;
                        return true;
                    }
                }
            }
            catch
            {
                if(sqlConn != null)
                {
                    sqlConn.Close();
                }
            }
            connectionInfo = null;
            return false;
        }

        internal BackupConfigInfo GetBackupConfigInfo(CDataContainer dataContainer, SqlConnection sqlConnection, string databaseName)
        {
            BackupOperation backupOperation = new BackupOperation();
            backupOperation.Initialize(dataContainer, sqlConnection);
            return backupOperation.CreateBackupConfigInfo(databaseName);
        }

        internal BackupOperation SetBackupInput(CDataContainer dataContainer, SqlConnection sqlConnection, BackupInfo input)
        {
            BackupOperation backupOperation = new BackupOperation();
            backupOperation.Initialize(dataContainer, sqlConnection);
            backupOperation.SetBackupInput(input);
            return backupOperation;
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        internal void PerformBackup(BackupOperation backupOperation)
        {
            backupOperation.PerformBackup();
        }

        /// <summary>
        /// Create a backup script task
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        internal async Task<TaskResult> BackupScriptTaskAsync(SqlTask sqlTask)
        {
            sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
            IBackupOperation backupOperation = sqlTask.TaskMetadata.Data as IBackupOperation;
            TaskResult taskResult = new TaskResult();
            string script = "";

            if (backupOperation != null)
            {
                await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        script = backupOperation.ScriptBackup();
                        taskResult.TaskStatus = SqlTaskStatus.Succeeded;
                        sqlTask.AddScript(taskResult.TaskStatus, script);
                    }
                    catch (Exception ex)
                    {
                        taskResult.TaskStatus = SqlTaskStatus.Failed;
                        taskResult.ErrorMessage = ex.Message;
                        if (ex.InnerException != null)
                        {
                            taskResult.ErrorMessage += System.Environment.NewLine + ex.InnerException.Message;
                        }
                        sqlTask.AddMessage(taskResult.TaskStatus == SqlTaskStatus.Failed ? taskResult.ErrorMessage : SR.TaskCompleted,
                                   taskResult.TaskStatus);
                    }
                });
            }
            else
            {
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return taskResult;
        }
        
        /// <summary>
        /// Async task to execute backup
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        private async Task<TaskResult> PerformBackupTaskAsync(SqlTask sqlTask)
        {
            sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
            IBackupOperation backupOperation = sqlTask.TaskMetadata.Data as IBackupOperation;
            TaskResult result = new TaskResult();

            // Create a task to perform backup
            await Task.Factory.StartNew(() =>
            {
                if (backupOperation != null)
                {
                    try
                    {
                        backupOperation.PerformBackup();
                        result.TaskStatus = SqlTaskStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                        if (ex.InnerException != null)
                        {
                            result.ErrorMessage += System.Environment.NewLine + ex.InnerException.Message;
                        }
                    }
                }
                else
                {
                    result.TaskStatus = SqlTaskStatus.Failed;
                }
            });

            return result;
        }

        /// <summary>
        /// Async task to cancel backup
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        private async Task<TaskResult> CancelBackupTaskAsync(SqlTask sqlTask)
        {
            IBackupOperation backupOperation = sqlTask.TaskMetadata.Data as IBackupOperation;
            TaskResult result = new TaskResult();
            // Create a task for backup cancellation request
            await Task.Factory.StartNew(() =>
            {
                if (backupOperation != null)
                {
                    try
                    {
                        backupOperation.CancelBackup();
                        result.TaskStatus = SqlTaskStatus.Canceled;
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                    }
                }
                else
                {
                    result.TaskStatus = SqlTaskStatus.Failed;
                }
            });

            return result;
        }
    }
}
