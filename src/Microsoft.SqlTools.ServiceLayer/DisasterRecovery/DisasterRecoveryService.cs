//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery
{
    public class DisasterRecoveryService
    {
        private static readonly Lazy<DisasterRecoveryService> instance = new Lazy<DisasterRecoveryService>(() => new DisasterRecoveryService());
        private static ConnectionService connectionService = null;
        private BackupFactory backupFactory;

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DisasterRecoveryService()
        {
            this.backupFactory = new BackupFactory();
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
            serviceHost.SetRequestHandler(ExtendedDatabaseInfoRequest.Type, HandleExtendedDatabaseInfoRequest);
            // Create backup
            serviceHost.SetRequestHandler(BackupRequest.Type, HandleBackupRequest);
        }

        public static async Task HandleExtendedDatabaseInfoRequest(
            DefaultDatabaseInfoParams optionsParams,
            RequestContext<ExtendedDatabaseInfoResponse> requestContext)
        {
            var response = new ExtendedDatabaseInfoResponse();
            ConnectionInfo connInfo;
            DisasterRecoveryService.ConnectionServiceInstance.TryFindConnection(
                    optionsParams.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                CDataContainer dataContainer = GetDataContainer(connInfo);
                SqlConnection sqlConn = GetSqlConnection(connInfo);
                if (sqlConn != null)
                {
                    DisasterRecoveryService.Instance.InitializeBackup(dataContainer, sqlConn);
                    ExtendedDatabaseInfo extendedDatabaseInfo = DisasterRecoveryService.Instance.GetDatabaseInfo(sqlConn.Database);
                    DatabaseTaskHelper taskHelper = AdminService.CreateDatabaseTaskHelper(connInfo);
                    DatabaseInfo defaultDatabaseInfo = DatabaseTaskHelper.DatabasePrototypeToDatabaseInfo(taskHelper.Prototype);
                    extendedDatabaseInfo.Options = defaultDatabaseInfo.Options;
                    response.ExtendedDatabaseInfo = extendedDatabaseInfo;
                }
            }
            
            await requestContext.SendResult(response);
        }
        
        internal static CDataContainer GetDataContainer(ConnectionInfo connInfo)
        {
            CDataContainer dataContainer = null;

            if (connInfo != null)
            {
                char[] passwordArray = connInfo.ConnectionDetails.Password.ToCharArray();
                if (string.Equals(connInfo.ConnectionDetails.AuthenticationType, "SqlLogin", StringComparison.OrdinalIgnoreCase))
                {
                    unsafe
                    {
                        fixed (char* passwordPtr = passwordArray)
                        {
                            dataContainer = new CDataContainer(
                            CDataContainer.ServerType.SQL,
                            connInfo.ConnectionDetails.ServerName,
                            false,
                            connInfo.ConnectionDetails.UserName,
                            new System.Security.SecureString(passwordPtr, passwordArray.Length),
                            string.Empty);
                        }
                    }
                }
                else
                {
                    dataContainer = new CDataContainer(
                    CDataContainer.ServerType.SQL,
                    connInfo.ConnectionDetails.ServerName,
                    true,
                    null,
                    null,
                    null);
                }
            }

            return dataContainer;
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
            CDataContainer dataContainer;

            if (connInfo != null)
            {
                dataContainer = GetDataContainer(connInfo);
                SqlConnection sqlConn = GetSqlConnection(connInfo);
                if (sqlConn != null)
                {
                    DisasterRecoveryService.Instance.InitializeBackup(dataContainer, sqlConn);
                    DisasterRecoveryService.Instance.SetBackupInput(backupParams.BackupInfo);
                    DisasterRecoveryService.Instance.PerformBackup();
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

        private void InitializeBackup(CDataContainer dataContainer, SqlConnection sqlConnection)
        {
            this.backupFactory.Initialize(dataContainer, sqlConnection);
        }

        private void SetBackupInput(BackupInfo input)
        {   
            this.backupFactory.SetBackupInput(input);
        }

        private void PerformBackup()
        {
            this.backupFactory.PerformBackup();
        }
        
        private ExtendedDatabaseInfo GetDatabaseInfo(string databaseName)
        {
            return this.backupFactory.GetDatabaseInfo(databaseName);
        }   
        
    }
}
