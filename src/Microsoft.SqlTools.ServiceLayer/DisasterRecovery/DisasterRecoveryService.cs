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
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    /// <summary>
    /// Service for Backup and Restore
    /// </summary>
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());
        private static ConnectionService connectionService = null;
        private SqlTaskManager sqlTaskManagerInstance = null;
        private RestoreDatabaseHelper restoreDatabaseService = new RestoreDatabaseHelper();

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

        private SqlTaskManager SqlTaskManagerInstance
        {
            get
            {
                if (sqlTaskManagerInstance == null)
                {
                    sqlTaskManagerInstance = SqlTaskManager.Instance;
                }
                return sqlTaskManagerInstance;
            }
            set
            {
                sqlTaskManagerInstance = value;
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

            // Create respore config
            serviceHost.SetRequestHandler(RestoreConfigInfoRequest.Type, HandleRestoreConfigInfoRequest);
        }

        /// <summary>
        /// Handle request to get backup configuration info
        /// </summary>
        /// <param name="optionsParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        internal async Task HandleBackupConfigInfoRequest(
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
                        BackupConfigInfo backupConfigInfo = this.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
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
                bool supported = IsBackupRestoreOperationSupported(restoreParams.OwnerUri, out connInfo);

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
        /// Handles a restore config info request
        /// </summary>
        internal async Task HandleRestoreConfigInfoRequest(
            RestoreConfigInfoRequestParams restoreConfigInfoParams,
            RequestContext<RestoreConfigInfoResponse> requestContext)
        {
            RestoreConfigInfoResponse response = new RestoreConfigInfoResponse();

            try
            {
                ConnectionInfo connInfo;
                bool supported = IsBackupRestoreOperationSupported(restoreConfigInfoParams.OwnerUri, out connInfo);

                if (supported && connInfo != null)
                {
                    response = this.restoreDatabaseService.CreateConfigInfoResponse(restoreConfigInfoParams);
                }
                else
                {
                    response.ErrorMessage = SR.RestoreNotSupported;
                }
                await requestContext.SendResult(response);
            }
            catch (Exception ex)
            {
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
                bool supported = IsBackupRestoreOperationSupported(restoreParams.OwnerUri, out connInfo);

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
                            metadata.DatabaseName = restoreParams.TargetDatabaseName;
                            metadata.Name = SR.RestoreTaskName;
                            metadata.IsCancelable = true;
                            metadata.Data = restoreDataObject;

                            // create restore task and perform
                            SqlTask sqlTask = SqlTaskManagerInstance.CreateAndRun(metadata, this.restoreDatabaseService.RestoreTaskAsync, restoreDatabaseService.CancelTaskAsync);
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
        internal async Task HandleBackupRequest(
            BackupParams backupParams,
            RequestContext<BackupResponse> requestContext)
        {
            try
            {
                BackupResponse response = new BackupResponse();
                ConnectionInfo connInfo;
                bool supported = IsBackupRestoreOperationSupported(backupParams.OwnerUri, out connInfo);

                if (supported && connInfo != null)
                {
                    DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true);
                    SqlConnection sqlConn = GetSqlConnection(connInfo);

                    BackupOperation backupOperation = CreateBackupOperation(helper.DataContainer, sqlConn, backupParams.BackupInfo);
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
                    }
                    else
                    {
                        metadata.Name = SR.BackupTaskName;
                        metadata.TaskExecutionMode = TaskExecutionMode.ExecuteAndScript;
                    }

                    sqlTask = SqlTaskManagerInstance.CreateAndRun(metadata, this.PerformBackupTaskAsync, this.CancelBackupTaskAsync);
                }
                else
                {
                    response.Result = false;
                }

                await requestContext.SendResult(response);
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

        private bool IsBackupRestoreOperationSupported(string ownerUri, out ConnectionInfo connectionInfo)
        {
            SqlConnection sqlConn = null;
            try
            {
                ConnectionInfo connInfo;
                DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                        ownerUri,
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

        private BackupOperation CreateBackupOperation(CDataContainer dataContainer, SqlConnection sqlConnection)
        {
            BackupOperation backupOperation = new BackupOperation();
            backupOperation.Initialize(dataContainer, sqlConnection);
            return backupOperation;
        }

        internal BackupOperation CreateBackupOperation(CDataContainer dataContainer, SqlConnection sqlConnection, BackupInfo input)
        {
            BackupOperation backupOperation = CreateBackupOperation(dataContainer, sqlConnection);
            backupOperation.SetBackupInput(input);
            return backupOperation;
        }

        internal BackupConfigInfo GetBackupConfigInfo(CDataContainer dataContainer, SqlConnection sqlConnection, string databaseName)
        {
            BackupOperation backupOperation = CreateBackupOperation(dataContainer, sqlConnection);
            return backupOperation.CreateBackupConfigInfo(databaseName);
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        internal void PerformBackup(BackupOperation backupOperation)
        {
            backupOperation.Execute(TaskExecutionMode.ExecuteAndScript);
        }

        /// <summary>
        /// For testing purpose only
        /// </summary>
        internal void ScriptBackup(BackupOperation backupOperation)
        {
            backupOperation.Execute(TaskExecutionMode.Script);
        }

        /// <summary>
        /// Async task to execute backup
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        internal async Task<TaskResult> PerformBackupTaskAsync(SqlTask sqlTask)
        {
            IBackupOperation backupOperation = sqlTask.TaskMetadata.Data as IBackupOperation;
            TaskResult result = new TaskResult();

            // Create a task to perform backup
            await Task.Factory.StartNew(() =>
            {
                if (backupOperation != null)
                {
                    try
                    {
                        sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);

                        // Execute backup
                        backupOperation.Execute(sqlTask.TaskMetadata.TaskExecutionMode);

                        // Set result
                        result.TaskStatus = SqlTaskStatus.Succeeded;

                        // Send generated script to client
                        if (!String.IsNullOrEmpty(backupOperation.ScriptContent))
                        {
                            sqlTask.AddScript(result.TaskStatus, backupOperation.ScriptContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = string.Format(CultureInfo.InvariantCulture, "error:{0} inner:{1} stacktrace:{2}",
                            ex.Message,
                            ex.InnerException != null ? ex.InnerException.Message : "",
                            ex.StackTrace);
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
        internal async Task<TaskResult> CancelBackupTaskAsync(SqlTask sqlTask)
        {
            IBackupOperation backupOperation = sqlTask.TaskMetadata.Data as IBackupOperation;
            TaskResult result = new TaskResult();

            await Task.Factory.StartNew(() =>
            {
                if (backupOperation != null)
                {
                    try
                    {
                        backupOperation.Cancel();
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
