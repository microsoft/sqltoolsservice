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
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

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
        private FileBrowserService fileBrowserService = null;
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
        /// Gets or sets the current filebrowser service instance
        /// </summary>
        internal FileBrowserService FileBrowserServiceInstance
        {
            get
            {
                if (fileBrowserService == null)
                {
                    fileBrowserService = FileBrowserService.Instance;
                }
                return fileBrowserService;
            }
            set
            {
                fileBrowserService = value;
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

            // Create restore task
            serviceHost.SetRequestHandler(RestoreRequest.Type, HandleRestoreRequest);

            // Create restore plan
            serviceHost.SetRequestHandler(RestorePlanRequest.Type, HandleRestorePlanRequest);

            // Create restore config
            serviceHost.SetRequestHandler(RestoreConfigInfoRequest.Type, HandleRestoreConfigInfoRequest);

            // Register file path validation callbacks
            FileBrowserServiceInstance.RegisterValidatePathsCallback(FileValidationServiceConstants.Backup, DisasterRecoveryFileValidator.ValidatePaths);
            FileBrowserServiceInstance.RegisterValidatePathsCallback(FileValidationServiceConstants.Restore, DisasterRecoveryFileValidator.ValidatePaths);
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
                    using (DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true))
                    {
                        using (SqlConnection sqlConn = ConnectionServiceInstance.OpenSqlConnection(connInfo))
                        {
                            if (sqlConn != null && !connInfo.IsSqlDW && !connInfo.IsAzure)
                            {
                                BackupConfigInfo backupConfigInfo = this.GetBackupConfigInfo(helper.DataContainer, sqlConn, sqlConn.Database);
                                backupConfigInfo.DatabaseInfo = AdminService.GetDatabaseInfo(connInfo);
                                response.BackupConfigInfo = backupConfigInfo;
                            }
                        }
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
                       
                        RestoreDatabaseTaskDataObject restoreDataObject = this.restoreDatabaseService.CreateRestoreDatabaseTaskDataObject(restoreParams, connInfo);

                        if (restoreDataObject != null)
                        {
                            restoreDataObject.LockedDatabaseManager = ConnectionServiceInstance.LockedDatabaseManager;
                            // create task metadata
                            TaskMetadata metadata = TaskMetadata.Create(restoreParams, SR.RestoreTaskName, restoreDataObject, ConnectionServiceInstance);
                            metadata.DatabaseName = restoreParams.TargetDatabaseName;

                            // create restore task and perform
                            SqlTask sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);
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
                    SqlConnection sqlConn = ConnectionServiceInstance.OpenSqlConnection(connInfo);
                    // Connection gets discounnected when backup is done

                    BackupOperation backupOperation = CreateBackupOperation(helper.DataContainer, sqlConn, backupParams.BackupInfo);
                    SqlTask sqlTask = null;

                    // create task metadata
                    TaskMetadata metadata = TaskMetadata.Create(backupParams, SR.BackupTaskName, backupOperation, ConnectionServiceInstance);
                   
                    sqlTask = SqlTaskManagerInstance.CreateAndRun<SqlTask>(metadata);
                    sqlTask.StatusChanged += CloseConnection;
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
                    using (sqlConn = ConnectionServiceInstance.OpenSqlConnection(connInfo))
                    {
                        if (sqlConn != null && !connInfo.IsSqlDW && !connInfo.IsAzure)
                        {
                            connectionInfo = connInfo;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                if(sqlConn != null && sqlConn.State == System.Data.ConnectionState.Open)
                {
                    sqlConn.Close();
                }
            }
            connectionInfo = null;
            return false;
        }

        private void CloseConnection(object sender, TaskEventArgs<SqlTaskStatus> e)
        {
            SqlTask sqlTask = e.SqlTask;
            if (sqlTask != null && sqlTask.IsCompleted)
            {
                connectionService.Disconnect(new DisconnectParams()
                {
                    OwnerUri = sqlTask.TaskMetadata.OwnerUri
                });
            }
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
    }
}
