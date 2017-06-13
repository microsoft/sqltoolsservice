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

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());
        private static ConnectionService connectionService = null;
        private BackupUtilities backupUtilities;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DisasterRecoveryService()
        {
            this.backupUtilities = new BackupUtilities();
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
        public void InitializeService(ServiceHost serviceHost)
        {
            // Get database info
            serviceHost.SetRequestHandler(BackupConfigInfoRequest.Type, HandleBackupConfigInfoRequest);
            // Create backup
            serviceHost.SetRequestHandler(BackupRequest.Type, HandleBackupRequest);
        }

        public static async Task HandleBackupConfigInfoRequest(
            DefaultDatabaseInfoParams optionsParams,
            RequestContext<BackupConfigInfoResponse> requestContext)
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
                if (sqlConn != null)
                {
                    DisasterRecoveryService.Instance.InitializeBackup(helper.DataContainer, sqlConn);
                    BackupConfigInfo backupConfigInfo = DisasterRecoveryService.Instance.GetBackupConfigInfo(sqlConn.Database);
                    backupConfigInfo.DatabaseInfo = AdminService.GetDatabaseInfo(connInfo);
                    response.BackupConfigInfo = backupConfigInfo;                
                }
            }
            
            await requestContext.SendResult(response);
        }

        /// <summary>
        /// Handles a backup request
        /// </summary>
        internal static async Task HandleBackupRequest(
            BackupParams backupParams,
            RequestContext<BackupResponse> requestContext)
        {            
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    backupParams.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true);
                SqlConnection sqlConn = GetSqlConnection(connInfo);
                if (sqlConn != null)
                {
                    // initialize backup
                    DisasterRecoveryService.Instance.InitializeBackup(helper.DataContainer, sqlConn);
                    DisasterRecoveryService.Instance.SetBackupInput(backupParams.BackupInfo);

                    // create task metadata
                    TaskMetadata metadata = new TaskMetadata();
                    metadata.ServerName = connInfo.ConnectionDetails.ServerName;
                    metadata.DatabaseName = connInfo.ConnectionDetails.DatabaseName;                    
                    metadata.Name = "Backup Database";
                    metadata.Description = "Backup Database";
                    metadata.IsCancelable = true;

                    // create backup task and perform
                    SqlTask sqlTask = TaskService.Instance.TaskManager.CreateTask(metadata, Instance.BackupTask);
                    sqlTask.Run();                    
                }
            }
         
            await requestContext.SendResult(new BackupResponse());
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

        internal void InitializeBackup(CDataContainer dataContainer, SqlConnection sqlConnection)
        {
            this.backupUtilities.Initialize(dataContainer, sqlConnection);
        }

        internal void SetBackupInput(BackupInfo input)
        {   
            this.backupUtilities.SetBackupInput(input);
        }

        internal void PerformBackup()
        {
            this.backupUtilities.PerformBackup();
        }

        internal BackupConfigInfo GetBackupConfigInfo(string databaseName)
        {
            return this.backupUtilities.GetBackupConfigInfo(databaseName);
        }

        public async Task<TaskResult> BackupTask(SqlTask sqlTask)
        {                           
            return await await Task.Factory.StartNew(async () =>
            {
                sqlTask.AddMessage("In progress", SqlTaskStatus.InProgress, true);

                // create a task to perform backup
                Task<TaskResult> backupTask = Task.Factory.StartNew(() =>
                {
                    TaskResult result = new TaskResult();
                    try
                    {
                        this.backupUtilities.PerformBackup();
                        result.TaskStatus = SqlTaskStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                    }
                    return result;
                });

                // create a task for backup cancellation request
                Task<TaskResult> cancelTask = Task.Factory.StartNew(() =>
                {
                    TaskResult result = new TaskResult();
                    while (true)
                    {
                        if (sqlTask.IsCancelRequested)
                        {
                            break;
                        }
                    };

                    try
                    {
                        this.backupUtilities.CancelBackup();
                        result.TaskStatus = SqlTaskStatus.Canceled;                        
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                    }

                    return result;
                });

                Task<TaskResult> completedTask = await Task.WhenAny(backupTask, cancelTask);
                                
                sqlTask.AddMessage("Finished", completedTask.Result.TaskStatus);
                return completedTask.Result;                
            });
        }

    }
}
