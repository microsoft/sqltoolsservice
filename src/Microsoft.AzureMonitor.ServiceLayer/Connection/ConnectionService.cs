using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AzureMonitor.ServiceLayer.DataSource;
using Microsoft.AzureMonitor.ServiceLayer.Localization;
using Microsoft.SqlTools.Hosting.DataContracts.Connection;
using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.Connection
{
    public class ConnectionService
    {
        private readonly ConcurrentDictionary<string, MonitorDataSource> _connectionByOwner;
        private IProtocolEndpoint _serviceHost;
        private IDataSourceFactory _dataSourceFactory;
        private static readonly Lazy<ConnectionService> _instance
            = new Lazy<ConnectionService>(() => new ConnectionService());
        
        public static ConnectionService Instance => _instance.Value;
        
        public ConnectionService()
        {
            _connectionByOwner = new ConcurrentDictionary<string, MonitorDataSource>();
        }

        public void InitializeService(IProtocolEndpoint serviceHost, IDataSourceFactory dataSourceFactory)
        {
            _serviceHost = serviceHost;
            _dataSourceFactory = dataSourceFactory;
            
            serviceHost.SetRequestHandler(ConnectionRequest.Type, HandleConnectRequest);
            serviceHost.SetRequestHandler(CancelConnectRequest.Type, HandleCancelConnectRequest);
            serviceHost.SetRequestHandler(DisconnectRequest.Type, HandleDisconnectRequest);
            serviceHost.SetRequestHandler(ListDatabasesRequest.Type, HandleListDatabasesRequest);
            serviceHost.SetRequestHandler(ChangeDatabaseRequest.Type, HandleChangeDatabaseRequest);
        }

        /// <summary>
        /// Handle new connection requests
        /// </summary>
        /// <param name="connectParams"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        private async Task HandleConnectRequest(ConnectParams connectParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleConnectRequest");
            try
            {
                Parallel.Invoke(async () =>
                {
                    await ValidateAndConnect(connectParams);
                });
                await requestContext.SendResult(true);
            }
            catch (Exception ex)
            {
                var result = new ConnectionCompleteParams()
                {
                    Messages = ex.ToString()
                };

                await _serviceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                await requestContext.SendResult(false);
            }
        }

        private async Task ValidateAndConnect(ConnectParams connectParams)
        {
            // result is null if the ConnectParams was successfully validated 
            ConnectionCompleteParams errorResult = ValidateConnectParams(connectParams);
            if (errorResult != null)
            {
                await _serviceHost.SendEvent(ConnectionCompleteNotification.Type, errorResult);
                return;
            }

            // open connection based on request details
            var connectionResult = Connect(connectParams);
            await _serviceHost.SendEvent(ConnectionCompleteNotification.Type, connectionResult);
        }

        /// <summary>
        /// Validates the given ConnectParams object. 
        /// </summary>
        /// <param name="connectionParams">The params to validate</param>
        /// <returns>A ConnectionCompleteParams object upon validation error, 
        /// null upon validation success</returns>
        private ConnectionCompleteParams ValidateConnectParams(ConnectParams connectionParams)
        {
            if (connectionParams == null)
            {
                return new ConnectionCompleteParams
                {
                    Messages = SR.ConnectionServiceConnectErrorNullParams
                };
            }
            if (!ConnectionServiceHelper.IsValid(connectionParams, out string paramValidationErrorMessage))
            {
                return new ConnectionCompleteParams
                {
                    OwnerUri = connectionParams.OwnerUri,
                    Messages = paramValidationErrorMessage
                };
            }

            // return null upon success
            return null;
        }

        public ConnectionCompleteParams Connect(ConnectParams connectionParams)
        {
            if (!_connectionByOwner.TryGetValue(connectionParams.OwnerUri, out MonitorDataSource datasource))
            {
                datasource = _dataSourceFactory.Create(connectionParams.Connection);
                _connectionByOwner.TryAdd(connectionParams.OwnerUri, datasource);    
            }

            return new ConnectionCompleteParams
            {
                ConnectionId = Guid.NewGuid().ToString(),
                OwnerUri = connectionParams.OwnerUri,
                ConnectionSummary = new ConnectionSummary
                {
                    ServerName = datasource.ServerName,
                    DatabaseName = datasource.DatabaseName,
                    UserName = connectionParams.Connection.UserName
                },
                ServerInfo = new ServerInfo
                {
                    // todo JM Server properties for Manage dashboard
                    Options = new Dictionary<string, object>(),
                    IsCloud = true
                }
            };
        }

        private async Task HandleCancelConnectRequest(CancelConnectParams cancelParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCancelConnectRequest");
            
            try
            {
                bool result = CancelOrDisconnect(cancelParams.OwnerUri);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private async Task HandleDisconnectRequest(DisconnectParams disconnectParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleDisconnectRequest");

            try
            {
                bool result = CancelOrDisconnect(disconnectParams.OwnerUri);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
        
        private async Task HandleListDatabasesRequest(ListDatabasesParams listDatabasesParams, RequestContext<ListDatabasesResponse> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "ListDatabasesRequest");

            try
            {
                ListDatabasesResponse result = ListDatabases(listDatabasesParams);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        private ListDatabasesResponse ListDatabases(ListDatabasesParams listDatabasesParams)
        {
            // Verify parameters
            if (string.IsNullOrEmpty(listDatabasesParams.OwnerUri))
            {
                throw new ArgumentException(SR.ConnectionServiceListDbErrorNullOwnerUri);
            }

            // Use the existing connection as a base for the search
            if (!_connectionByOwner.TryGetValue(listDatabasesParams.OwnerUri, out MonitorDataSource datasource))
            {
                throw new Exception(SR.ConnectionServiceListDbErrorNotConnected(listDatabasesParams.OwnerUri));
            }
            
            List<ObjectMetadata> databases = datasource.GetDatabases(true);
            
            // Mainly used by "Manage" dashboard
            if (listDatabasesParams.IncludeDetails.HasTrue())
            {
                return new ListDatabasesResponse
                {
                    Databases = ConvertToDatabaseInfo(databases)
                };
            }
            
            var databaseNames = databases
                    .Select(objMeta => objMeta.PrettyName == objMeta.Name ? objMeta.PrettyName : $"{objMeta.PrettyName} ({objMeta.Name})")
                    .ToArray();

            return new ListDatabasesResponse
            {
                DatabaseNames = databaseNames
            };
        }

        private DatabaseInfo[] ConvertToDatabaseInfo(List<ObjectMetadata> objectMetadatas)
        {
            var databaseInfos = new List<DatabaseInfo>();
            foreach (var metadata in objectMetadatas)
            {
                var databaseInfo = new DatabaseInfo();
                databaseInfo.Options["name"] = metadata.Name;
                databaseInfo.Options["sizeInMB"] = (metadata.SizeInMb / (1024 * 1024)).ToString();

                databaseInfos.Add(databaseInfo);
            }

            return databaseInfos.ToArray();
        }

        private async Task HandleChangeDatabaseRequest(ChangeDatabaseParams changeDatabaseParams, RequestContext<bool> requestContext)
        {
            if (!_connectionByOwner.TryGetValue(changeDatabaseParams.OwnerUri, out MonitorDataSource datasource))
            {
                await requestContext.SendResult(false);
                return;
            }

            datasource.ChangeWorkspace(changeDatabaseParams.NewDatabase);

            var returnParameters = new ConnectionChangedParams
            {
                OwnerUri = changeDatabaseParams.OwnerUri,
                Connection = new ConnectionSummary
                {
                    DatabaseName = datasource.DatabaseName,
                    ServerName = datasource.ServerName,
                    UserName = datasource.UserName
                }
            };

            await _serviceHost.SendEvent(ConnectionChangedNotification.Type, returnParameters);
            await requestContext.SendResult(true);
        }

        public bool CancelOrDisconnect(string ownerUri)
        {
            // Validate parameters
            if (ownerUri == null || string.IsNullOrEmpty(ownerUri))
            {
                return false;
            }
            
            // Lookup the ConnectionInfo owned by the URI
            return _connectionByOwner.TryRemove(ownerUri, out _);
        }

        public MonitorDataSource GetDataSource(string ownerUri)
        {
            if (_connectionByOwner.TryGetValue(ownerUri, out MonitorDataSource datasource))
            {
                return datasource;
            }

            throw new Exception("Datasource not found");
        }
    }
}