//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Admin task service class
    /// </summary>
    public class AdminService
    {
        private static readonly Lazy<AdminService> instance = new Lazy<AdminService>(() => new AdminService());

        private static ConnectionService connectionService = null;

        private static readonly ConcurrentDictionary<string, DatabaseTaskHelper> serverTaskHelperMap =
            new ConcurrentDictionary<string, DatabaseTaskHelper>();

        /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal AdminService()
        {
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                AdminService.connectionService ??= ConnectionService.Instance;
                return AdminService.connectionService;
            }

            set
            {
                AdminService.connectionService = value;
            }
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AdminService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.RegisterRequestHandler(CreateDatabaseRequest.Type, HandleCreateDatabaseRequest);
            serviceHost.RegisterRequestHandler(CreateLoginRequest.Type, HandleCreateLoginRequest);
            serviceHost.RegisterRequestHandler(DefaultDatabaseInfoRequest.Type, HandleDefaultDatabaseInfoRequest);
            serviceHost.RegisterRequestHandler(GetDatabaseInfoRequest.Type, HandleGetDatabaseInfoRequest);
            serviceHost.RegisterRequestHandler(GetDataFolderRequest.Type, HandleGetDataFolderRequest);
            serviceHost.RegisterRequestHandler(GetBackupFolderRequest.Type, HandleGetBackupFolderRequest);
            serviceHost.RegisterRequestHandler(GetAssociatedFilesRequest.Type, HandleGetAssociatedFilesRequest);
        }

        /// <summary>
        /// Handle a request for the default database prototype info
        /// </summary>
        public static async Task<DefaultDatabaseInfoResponse> HandleDefaultDatabaseInfoRequest(
            DefaultDatabaseInfoParams optionsParams)
        {
            var response = new DefaultDatabaseInfoResponse();
            ConnectionInfo connInfo;
            AdminService.ConnectionServiceInstance.TryFindConnection(
                optionsParams.OwnerUri,
                out connInfo);

            using (var taskHelper = CreateDatabaseTaskHelper(connInfo))
            {
                response.DefaultDatabaseInfo = DatabaseTaskHelper.DatabasePrototypeToDatabaseInfo(taskHelper.Prototype);
                return response;
            }
        }

        /// <summary>
        /// Handles a create database request
        /// </summary>
        internal static async Task<CreateDatabaseResponse> HandleCreateDatabaseRequest(
            CreateDatabaseParams databaseParams)
        {
            var response = new DefaultDatabaseInfoResponse();
            ConnectionInfo connInfo;
            AdminService.ConnectionServiceInstance.TryFindConnection(
                databaseParams.OwnerUri,
                out connInfo);

            using (var taskHelper = CreateDatabaseTaskHelper(connInfo))
            {
                DatabasePrototype prototype = taskHelper.Prototype;
                DatabaseTaskHelper.ApplyToPrototype(databaseParams.DatabaseInfo, taskHelper.Prototype);

                Database db = prototype.ApplyChanges();

                return new CreateDatabaseResponse()
                {
                    Result = true,
                    TaskId = 0
                };
            }
        }

        /// <summary>
        /// Handle get database info request
        /// </summary>
        internal static async Task<GetDatabaseInfoResponse> HandleGetDatabaseInfoRequest(
            GetDatabaseInfoParams databaseParams)
        {
            Func<Task<GetDatabaseInfoResponse>> requestHandler = async () =>
            {
                ConnectionInfo connInfo;
                AdminService.ConnectionServiceInstance.TryFindConnection(
                        databaseParams.OwnerUri,
                        out connInfo);
                DatabaseInfo info = null;

                if (connInfo != null)
                {
                    info = GetDatabaseInfo(connInfo);
                }

                return new GetDatabaseInfoResponse()
                {
                    DatabaseInfo = info
                };
            };

            try
            {
                return await requestHandler();
            }
            catch (Exception ex)
            {
                throw RpcErrorException.Create(ex.ToString());
            }
        }

        /// <summary>
        /// Handle get database data folder info request
        /// </summary>
        internal static async Task<string> HandleGetDataFolderRequest(
            GetDataFolderParams databaseParams)
        {
            Func<Task<string>> requestHandler = async () =>
            {
                ConnectionInfo connInfo;
                AdminService.ConnectionServiceInstance.TryFindConnection(
                        databaseParams.ConnectionUri,
                        out connInfo);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo))
                {
                    // Connection gets disconnected when backup is done
                    ServerConnection serverConnection = new ServerConnection(sqlConn);
                    var dataFolder = CommonUtilities.GetDefaultDataFolder(serverConnection);
                    return dataFolder;
                }
            };

            try
            {
                return await requestHandler();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                };
                throw RpcErrorException.Create(ex.Message);
            }
        }

        /// <summary>
        /// Handle get database backup folder info request
        /// </summary>
        internal static async Task<string> HandleGetBackupFolderRequest(
            GetBackupFolderParams databaseParams)
        {
            Func<Task<string>> requestHandler = async () =>
            {
                ConnectionInfo connInfo;
                AdminService.ConnectionServiceInstance.TryFindConnection(
                        databaseParams.ConnectionUri,
                        out connInfo);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo))
                {
                    // Connection gets disconnected when backup is done
                    ServerConnection serverConnection = new ServerConnection(sqlConn);
                    var backupFolder = CommonUtilities.GetDefaultBackupFolder(serverConnection);
                    return backupFolder;
                }
            };

            try
            {
                return await requestHandler();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                };
                throw RpcErrorException.Create(ex.Message);
            }
        }

        /// <summary>
        /// Handle get associated database files request
        /// </summary>
        internal static async Task<string[]> HandleGetAssociatedFilesRequest(
            GetAssociatedFilesParams databaseParams)
        {
            Func<Task<string[]>> requestHandler = async () =>
            {
                ConnectionInfo connInfo;
                AdminService.ConnectionServiceInstance.TryFindConnection(
                        databaseParams.ConnectionUri,
                        out connInfo);
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo))
                {
                    // Connection gets disconnected when backup is done
                    ServerConnection serverConnection = new ServerConnection(sqlConn);
                    var files = CommonUtilities.GetAssociatedFilePaths(serverConnection, databaseParams.PrimaryFilePath);
                    return files;
                }
            };

            try
            {
                return await requestHandler();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                };
                throw RpcErrorException.Create(ex.Message);
            }
        }

        /// <summary>
        /// Return database info for a specific database
        /// </summary>
        /// <param name="connInfo"></param>
        /// <returns></returns>
        internal static DatabaseInfo GetDatabaseInfo(ConnectionInfo connInfo)
        {
            using (DatabaseTaskHelper taskHelper = CreateDatabaseTaskHelper(connInfo))
            {
                return DatabaseTaskHelper.DatabasePrototypeToDatabaseInfo(taskHelper.Prototype);
            }
        }

        /// <summary>
        /// Create database task helper
        /// </summary>
        /// <param name="connInfo">connection info</param>
        /// <param name="databaseExists">flag indicating whether to create taskhelper for existing database or not</param>
        /// <returns></returns>
        internal static DatabaseTaskHelper CreateDatabaseTaskHelper(ConnectionInfo connInfo, bool databaseExists = false)
        {
            var dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists);
            var taskHelper = new DatabaseTaskHelper(dataContainer);
            return taskHelper;
        }

        /// <summary>
        /// Handles a create login request
        /// </summary>
        internal static async Task<CreateLoginResponse> HandleCreateLoginRequest(
            CreateLoginParams loginParams)
        {
            return new CreateLoginResponse();
        }
    }
}
