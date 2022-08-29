//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlServer.Management.Common;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    /// <summary>
    /// Main class for the Connection Management services
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private const string PasswordPlaceholder = "******";

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static readonly Lazy<ConnectionService> _instance
            = new Lazy<ConnectionService>(() => new ConnectionService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static ConnectionService Instance => _instance.Value;

        private DatabaseLocksManager lockedDatabaseManager;

        /// <summary>
        /// A map containing all CancellationTokenSource objects that are associated with a given URI/ConnectionType pair. 
        /// Entries in this map correspond to ReliableDataSourceClient instances that are in the process of connecting. 
        /// </summary>
        private readonly ConcurrentDictionary<CancelTokenKey, CancellationTokenSource> cancelTupleToCancellationTokenSourceMap =
                    new ConcurrentDictionary<CancelTokenKey, CancellationTokenSource>();

        private readonly object cancellationTokenSourceLock = new object();

        private ConcurrentDictionary<string, IConnectedBindingQueue> connectedQueues = new ConcurrentDictionary<string, IConnectedBindingQueue>();

        /// <summary>
        /// Database Lock manager instance
        /// </summary>
        internal DatabaseLocksManager LockedDatabaseManager
        {
            get
            {
                if (lockedDatabaseManager == null)
                {
                    lockedDatabaseManager = DatabaseLocksManager.Instance;
                }
                return lockedDatabaseManager;
            }
            set
            {
                this.lockedDatabaseManager = value;
            }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// </summary>
        private IProtocolEndpoint _serviceHost;


        /// <summary>
        /// Register a new connection queue if not already registered
        /// </summary>
        /// <param name="type"></param>
        /// <param name="connectedQueue"></param>
        public virtual void RegisterConnectedQueue(string type, IConnectedBindingQueue connectedQueue)
        {
            if (!connectedQueues.ContainsKey(type))
            {
                connectedQueues.AddOrUpdate(type, connectedQueue, (key, old) => connectedQueue);
            }
        }

        /// <summary>
        /// Callback for onconnection handler
        /// </summary>
        /// <param name="sqlConnection"></param>
        public delegate Task OnConnectionHandler(ConnectionInfo info);

        /// <summary>
        /// Callback for ondisconnect handler
        /// </summary>
        public delegate Task OnDisconnectHandler(IConnectionSummary summary, string ownerUri);

        /// <summary>
        /// List of onconnection handlers
        /// </summary>
        private readonly List<OnConnectionHandler> onConnectionActivities = new List<OnConnectionHandler>();

        /// <summary>
        /// List of ondisconnect handlers
        /// </summary>
        private readonly List<OnDisconnectHandler> onDisconnectActivities = new List<OnDisconnectHandler>();

        /// <summary>
        /// Gets the SQL connection factory instance
        /// </summary>
        private IDataSourceConnectionFactory _dataSourceConnectionFactory;

        private IConnectionManager _connectionManager;

        /// <summary>
        /// Validates the given ConnectParams object. 
        /// </summary>
        /// <param name="connectionParams">The params to validate</param>
        /// <returns>A ConnectionCompleteParams object upon validation error, 
        /// null upon validation success</returns>
        private ConnectionCompleteParams ValidateConnectParams(ConnectParams connectionParams)
        {
            string paramValidationErrorMessage;
            if (!connectionParams.IsValid(out paramValidationErrorMessage))
            {
                return new ConnectionCompleteParams
                {
                    ErrorMessage = paramValidationErrorMessage,
                    OwnerUri = connectionParams.OwnerUri
                };
            }

            // return null upon success
            return null;
        }

        /// <summary>
        /// Open a connection with the specified ConnectParams
        /// </summary>
        public async Task<ConnectionCompleteParams> Connect(ConnectParams connectionParams)
        {
            // Validate parameters
            ConnectionCompleteParams validationResults = ValidateConnectParams(connectionParams);
            if (validationResults != null)
            {
                return validationResults;
            }

            TrySetConnectionType(connectionParams);

            connectionParams.Connection.ApplicationName = GetApplicationNameWithFeature(connectionParams.Connection.ApplicationName, connectionParams.Purpose);
            // If there is no ConnectionInfo in the map, create a new ConnectionInfo, 
            // but wait until later when we are connected to add it to the map.
            bool connectionChanged = false;
            if (!_connectionManager.TryGetValue(connectionParams.OwnerUri, out ConnectionInfo connectionInfo))
            {
                connectionInfo = new ConnectionInfo(_dataSourceConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
            }
            else if (IsConnectionChanged(connectionParams, connectionInfo))
            {
                // We are actively changing the connection information for this connection. We must disconnect
                // all active connections, since it represents a full context change
                connectionChanged = true;
            }

            DisconnectExistingConnectionIfNeeded(connectionParams, connectionInfo, disconnectAll: connectionChanged);

            if (connectionChanged)
            {
                connectionInfo = new ConnectionInfo(_dataSourceConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
            }

            // Try to open a connection with the given ConnectParams
            ConnectionCompleteParams response = await TryOpenConnection(connectionInfo, connectionParams);
            if (response != null)
            {
                return response;
            }

            // If this is the first connection for this URI, add the ConnectionInfo to the map
            bool addToMap = connectionChanged || !_connectionManager.ContainsKey(connectionParams.OwnerUri);
            if (addToMap)
            {
                _connectionManager.TryAdd(connectionParams.OwnerUri, connectionInfo);
            }

            // Return information about the connected SQL Server instance
            ConnectionCompleteParams completeParams = GetConnectionCompleteParams(connectionParams.Type, connectionInfo);
            // Invoke callback notifications          
            InvokeOnConnectionActivities(connectionInfo, connectionParams);

            TryCloseConnectionTemporaryConnection(connectionParams, connectionInfo);

            return completeParams;
        }

        internal bool TryRefreshAuthToken(string ownerUri, out string token)
        {
            token = string.Empty;
            if (!_connectionManager.TryGetValue(ownerUri, out ConnectionInfo connection))
            {
                return false;
            }

            var requestMessage = new RequestSecurityTokenParams
            {
                AccountId = connection.ConnectionDetails.GetOptionValue("azureAccount", string.Empty),
                Authority = connection.ConnectionDetails.GetOptionValue("azureTenantId", string.Empty),
                Provider = connection.ConnectionDetails.AuthenticationType,
                Resource = "SQL"
            };

            var response = _serviceHost.SendRequest(SecurityTokenRequest.Type, requestMessage, true).Result;
            connection.UpdateAuthToken(response.Token);
            token = response.Token;
            return true;
        }

        private void TryCloseConnectionTemporaryConnection(ConnectParams connectionParams, ConnectionInfo connectionInfo)
        {
            try
            {
                if (connectionParams.Purpose == ConnectionType.ObjectExplorer || connectionParams.Purpose == ConnectionType.Dashboard || connectionParams.Purpose == ConnectionType.GeneralConnection)
                {
                    ReliableDataSourceConnection connection;
                    string type = connectionParams.Type;
                    if (connectionInfo.TryGetConnection(type, out connection))
                    {
                        // OE doesn't need to keep the connection open
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Information, "Failed to close temporary connections. error: " + ex.Message);
            }
        }

        private static string GetApplicationNameWithFeature(string applicationName, string featureName)
        {
            string appNameWithFeature = applicationName;

            if (!string.IsNullOrWhiteSpace(applicationName) && !string.IsNullOrWhiteSpace(featureName))
            {
                int index = applicationName.IndexOf('-');
                string appName = applicationName;
                if (index > 0)
                {
                    appName = applicationName.Substring(0, index);
                }
                appNameWithFeature = $"{appName}-{featureName}";
            }

            return appNameWithFeature;
        }

        private void TrySetConnectionType(ConnectParams connectionParams)
        {
            if (connectionParams != null && connectionParams.Type == ConnectionType.Default && !string.IsNullOrWhiteSpace(connectionParams.OwnerUri))
            {
                if (connectionParams.OwnerUri.ToLowerInvariant().StartsWith("dashboard://"))
                {
                    connectionParams.Purpose = ConnectionType.Dashboard;
                }
                else if (connectionParams.OwnerUri.ToLowerInvariant().StartsWith("connection://"))
                {
                    connectionParams.Purpose = ConnectionType.GeneralConnection;
                }
            }
            else if (connectionParams != null)
            {
                connectionParams.Purpose = connectionParams.Type;
            }
        }

        private bool IsConnectionChanged(ConnectParams connectionParams, ConnectionInfo connectionInfo)
        {
            if (connectionInfo.HasConnectionType(connectionParams.Type)
                && !connectionInfo.ConnectionDetails.IsComparableTo(connectionParams.Connection))
            {
                return true;
            }
            return false;
        }

        private void DisconnectExistingConnectionIfNeeded(ConnectParams connectionParams, ConnectionInfo connectionInfo, bool disconnectAll)
        {
            // Resolve if it is an existing connection
            // Disconnect active connection if the URI is already connected for this connection type
            ReliableDataSourceConnection existingConnection;
            if (connectionInfo.TryGetConnection(connectionParams.Type, out existingConnection))
            {
                var disconnectParams = new DisconnectParams()
                {
                    OwnerUri = connectionParams.OwnerUri,
                    Type = disconnectAll ? null : connectionParams.Type
                };
                Disconnect(disconnectParams);
            }
        }

        /// <summary>
        /// Creates a ConnectionCompleteParams as a response to a successful connection. 
        /// Also sets the DatabaseName and IsAzure properties of ConnectionInfo.
        /// </summary>
        /// <returns>A ConnectionCompleteParams in response to the successful connection</returns>
        private ConnectionCompleteParams GetConnectionCompleteParams(string connectionType, ConnectionInfo connectionInfo)
        {
            ConnectionCompleteParams response = new ConnectionCompleteParams { OwnerUri = connectionInfo.OwnerUri, Type = connectionType };

            try
            {
                ReliableDataSourceConnection connection;
                connectionInfo.TryGetConnection(connectionType, out connection);

                // Update with the actual database name in connectionInfo and result
                // Doing this here as we know the connection is open - expect to do this only on connecting
                connectionInfo.ConnectionDetails.DatabaseName = connection.Database;
                if (!string.IsNullOrEmpty(connectionInfo.ConnectionDetails.ConnectionString))
                {
                    // If the connection was set up with a connection string, use the connection string to get the details
                    var connectionStringBuilder = DataSourceFactory.CreateConnectionStringBuilder(DataSourceType.Kusto, connection.ConnectionString);
                    
                    response.ConnectionSummary = new ConnectionSummary
                    {
                        ServerName = connectionStringBuilder.DataSource,
                        DatabaseName = connectionStringBuilder.InitialCatalog,
                        UserName = connectionStringBuilder.UserID
                    };
                }
                else
                {
                    response.ConnectionSummary = new ConnectionSummary
                    {
                        ServerName = connectionInfo.ConnectionDetails.ServerName,
                        DatabaseName = connectionInfo.ConnectionDetails.DatabaseName,
                        UserName = connectionInfo.ConnectionDetails.UserName
                    };
                }

                response.ConnectionId = connectionInfo.ConnectionId.ToString();

                var reliableConnection = connection as ReliableDataSourceConnection;
                IDataSource dataSource = reliableConnection.GetUnderlyingConnection();
                DataSourceObjectMetadata clusterMetadata = MetadataFactory.CreateClusterMetadata(connectionInfo.ConnectionDetails.ServerName);

                DiagnosticsInfo clusterDiagnostics = dataSource.GetDiagnostics(clusterMetadata);
                ReliableConnectionHelper.ServerInfo serverInfo = DataSourceFactory.ConvertToServerInfoFormat(DataSourceType.Kusto, clusterDiagnostics);

                response.ServerInfo = new ServerInfo
                {
                    Options = serverInfo.Options        // Server properties are shown on "manage" dashboard.
                };
                connectionInfo.IsCloud = response.ServerInfo.IsCloud;
                connectionInfo.MajorVersion = response.ServerInfo.ServerMajorVersion;
            }
            catch (Exception ex)
            {
                response.Messages = ex.ToString();
            }

            return response;
        }

        /// <summary>
        /// Tries to create and open a connection with the given ConnectParams.
        /// </summary>
        /// <returns>null upon success, a ConnectionCompleteParams detailing the error upon failure</returns>
        private async Task<ConnectionCompleteParams> TryOpenConnection(ConnectionInfo connectionInfo, ConnectParams connectionParams)
        {
            CancellationTokenSource source = null;
            ReliableDataSourceConnection connection = null;
            CancelTokenKey cancelKey = new CancelTokenKey { OwnerUri = connectionParams.OwnerUri, Type = connectionParams.Type };
            ConnectionCompleteParams response = new ConnectionCompleteParams { OwnerUri = connectionInfo.OwnerUri, Type = connectionParams.Type };
            bool? currentPooling = connectionInfo.ConnectionDetails.Pooling;

            try
            {
                connectionInfo.ConnectionDetails.Pooling = false;

                // create a data source connection instance
                connection = connectionInfo.Factory.CreateDataSourceConnection(connectionInfo.ConnectionDetails, connectionInfo.OwnerUri);
                connectionInfo.AddConnection(connectionParams.Type, connection);

                // Add a cancellation token source so that the connection OpenAsync() can be cancelled
                source = new CancellationTokenSource();
                // Locking here to perform two operations as one atomic operation
                lock (cancellationTokenSourceLock)
                {
                    // If the URI is currently connecting from a different request, cancel it before we try to connect
                    CancellationTokenSource currentSource;
                    if (cancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out currentSource))
                    {
                        currentSource.Cancel();
                    }
                    cancelTupleToCancellationTokenSourceMap[cancelKey] = source;
                }

                // Open the connection
                await connection.OpenAsync(source.Token);
            }
            catch (SqlException ex)
            {
                response.ErrorNumber = ex.Number;
                response.ErrorMessage = ex.Message;
                response.Messages = ex.ToString();
                return response;
            }
            catch (OperationCanceledException)
            {
                // OpenAsync was cancelled
                response.Messages = SR.ConnectionServiceConnectionCanceled;
                return response;
            }
            catch (Exception ex)
            {
                response.ErrorMessage = ex.Message;
                response.Messages = ex.ToString();
                return response;
            }
            finally
            {
                // Remove our cancellation token from the map since we're no longer connecting
                // Using a lock here to perform two operations as one atomic operation
                lock (cancellationTokenSourceLock)
                {
                    // Only remove the token from the map if it is the same one created by this request
                    CancellationTokenSource sourceValue;
                    if (cancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out sourceValue) && sourceValue == source)
                    {
                        cancelTupleToCancellationTokenSourceMap.TryRemove(cancelKey, out sourceValue);
                    }
                    source?.Dispose();
                }
                if (connectionInfo != null && connectionInfo.ConnectionDetails != null)
                {
                    connectionInfo.ConnectionDetails.Pooling = currentPooling;
                }
            }

            // Return null upon success
            return null;
        }

        /// <summary>
        /// Gets the existing connection with the given URI and connection type string. If none exists, 
        /// creates a new connection. This cannot be used to create a default connection or to create a 
        /// connection if a default connection does not exist.
        /// </summary>
        /// <param name="ownerUri">URI identifying the resource mapped to this connection</param>
        /// <param name="connectionType">
        /// What the purpose for this connection is. A single resource
        /// such as a SQL file may have multiple connections - one for Intellisense, another for query execution
        /// </param>
        /// <param name="alwaysPersistSecurity">
        /// Workaround for .Net Core clone connection issues: should persist security be used so that
        /// when SMO clones connections it can do so without breaking on SQL Password connections.
        /// This should be removed once the core issue is resolved and clone works as expected
        /// </param>
        /// <returns>A DB connection for the connection type requested</returns>
        public virtual async Task<ReliableDataSourceConnection> GetOrOpenConnection(string ownerUri, string connectionType, bool alwaysPersistSecurity = false)
        {
            Validate.IsNotNullOrEmptyString(nameof(ownerUri), ownerUri);
            Validate.IsNotNullOrEmptyString(nameof(connectionType), connectionType);

            // Try to get the ConnectionInfo, if it exists
            ConnectionInfo connectionInfo;
            if (!_connectionManager.TryGetValue(ownerUri, out connectionInfo))
            {
                throw new ArgumentOutOfRangeException(SR.ConnectionServiceListDbErrorNotConnected(ownerUri));
            }

            // Make sure a default connection exists
            ReliableDataSourceConnection connection;
            ReliableDataSourceConnection defaultConnection;
            if (!connectionInfo.TryGetConnection(ConnectionType.Default, out defaultConnection))
            {
                throw new InvalidOperationException(SR.ConnectionServiceDbErrorDefaultNotConnected(ownerUri));
            }
            
            // Try to get the ReliableDataSourceClient and create if it doesn't already exist
            if (!connectionInfo.TryGetConnection(connectionType, out connection) && ConnectionType.Default != connectionType)
            {
                connection = await TryOpenConnectionForConnectionType(ownerUri, connectionType, alwaysPersistSecurity, connectionInfo);
            }

            return connection;
        }

        private async Task<ReliableDataSourceConnection> TryOpenConnectionForConnectionType(string ownerUri, string connectionType,
            bool alwaysPersistSecurity, ConnectionInfo connectionInfo)
        {
            // If the ReliableDataSourceClient does not exist and is not the default connection, create one.
            // We can't create the default (initial) connection here because we won't have a ConnectionDetails 
            // if Connect() has not yet been called.
            bool? originalPersistSecurityInfo = connectionInfo.ConnectionDetails.PersistSecurityInfo;
            if (alwaysPersistSecurity)
            {
                connectionInfo.ConnectionDetails.PersistSecurityInfo = true;
            }
            ConnectParams connectParams = new ConnectParams
            {
                OwnerUri = ownerUri,
                Connection = connectionInfo.ConnectionDetails,
                Type = connectionType
            };
            try
            {
                await Connect(connectParams);
            }
            finally
            {
                connectionInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;
            }

            ReliableDataSourceConnection connection;
            connectionInfo.TryGetConnection(connectionType, out connection);
            return connection;
        }

        /// <summary>
        /// Cancel a connection that is in the process of opening.
        /// </summary>
        public bool CancelConnect(CancelConnectParams cancelParams)
        {
            // Validate parameters
            if (cancelParams == null || string.IsNullOrEmpty(cancelParams.OwnerUri))
            {
                return false;
            }

            CancelTokenKey cancelKey = new CancelTokenKey
            {
                OwnerUri = cancelParams.OwnerUri,
                Type = cancelParams.Type
            };

            // Cancel any current connection attempts for this URI
            CancellationTokenSource source;
            if (cancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out source))
            {
                try
                {
                    source.Cancel();
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Close a connection with the specified connection details.
        /// </summary>
        public virtual bool Disconnect(DisconnectParams disconnectParams)
        {
            // Validate parameters
            if (disconnectParams == null || string.IsNullOrEmpty(disconnectParams.OwnerUri))
            {
                return false;
            }

            // Cancel if we are in the middle of connecting
            if (CancelConnections(disconnectParams.OwnerUri, disconnectParams.Type))
            {
                return false;
            }

            // Lookup the ConnectionInfo owned by the URI
            ConnectionInfo info;
            if (!_connectionManager.TryGetValue(disconnectParams.OwnerUri, out info))
            {
                return false;
            }

            // Call Close() on the connections we want to disconnect
            // If no connections were located, return false
            if (!CloseConnections(info, disconnectParams.Type))
            {
                return false;
            }

            // Remove the disconnected connections from the ConnectionInfo map
            if (disconnectParams.Type == null)
            {
                info.RemoveAllConnections();
            }
            else
            {
                info.RemoveConnection(disconnectParams.Type);
            }

            // If the ConnectionInfo has no more connections, remove the ConnectionInfo
            if (info.CountConnections == 0)
            {
                _connectionManager.TryRemove(disconnectParams.OwnerUri);
            }

            // Handle Telemetry disconnect events if we are disconnecting the default connection
            if (disconnectParams.Type == null || disconnectParams.Type == ConnectionType.Default)
            {
                HandleDisconnectTelemetry(info);
                InvokeOnDisconnectionActivities(info);
            }

            // Return true upon success
            return true;
        }

        /// <summary>
        /// Cancel connections associated with the given ownerUri.
        /// If connectionType is not null, cancel the connection with the given connectionType
        /// If connectionType is null, cancel all pending connections associated with ownerUri.
        /// </summary>
        /// <returns>true if a single pending connection associated with the non-null connectionType was 
        /// found and cancelled, false otherwise</returns>
        private bool CancelConnections(string ownerUri, string connectionType)
        {
            // Cancel the connection of the given type
            if (connectionType != null)
            {
                // If we are trying to disconnect a specific connection and it was just cancelled, 
                // this will return true
                return CancelConnect(new CancelConnectParams() { OwnerUri = ownerUri, Type = connectionType });
            }

            // Cancel all pending connections
            foreach (var entry in cancelTupleToCancellationTokenSourceMap)
            {
                string entryConnectionUri = entry.Key.OwnerUri;
                string entryConnectionType = entry.Key.Type;
                if (ownerUri.Equals(entryConnectionUri))
                {
                    CancelConnect(new CancelConnectParams() { OwnerUri = ownerUri, Type = entryConnectionType });
                }
            }

            return false;
        }

        /// <summary>
        /// Closes DbConnections associated with the given ConnectionInfo. 
        /// If connectionType is not null, closes the ReliableDataSourceClient with the type given by connectionType.
        /// If connectionType is null, closes all DbConnections.
        /// </summary>
        /// <returns>true if connections were found and attempted to be closed,
        /// false if no connections were found</returns>
        private bool CloseConnections(ConnectionInfo connectionInfo, string connectionType)
        {
            ICollection<ReliableDataSourceConnection> connectionsToDisconnect = new List<ReliableDataSourceConnection>();
            if (connectionType == null)
            {
                connectionsToDisconnect = connectionInfo.AllConnections;
            }
            else
            {
                // Make sure there is an existing connection of this type
                ReliableDataSourceConnection connection;
                if (!connectionInfo.TryGetConnection(connectionType, out connection))
                {
                    return false;
                }
                connectionsToDisconnect.Add(connection);
            }

            if (connectionsToDisconnect.Count == 0)
            {
                return false;
            }

            foreach (ReliableDataSourceConnection connection in connectionsToDisconnect)
            {
                try
                {
                    connection.Close();
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            return true;
        }

        /// <summary>
        /// List all databases on the server specified
        /// </summary>
        private ListDatabasesResponse ListDatabases(ListDatabasesParams listDatabasesParams)
        {
            // Verify parameters
            if (string.IsNullOrEmpty(listDatabasesParams.OwnerUri))
            {
                throw new ArgumentException(SR.ConnectionServiceListDbErrorNullOwnerUri);
            }

            // Use the existing connection as a base for the search
            if (!_connectionManager.TryGetValue(listDatabasesParams.OwnerUri, out ConnectionInfo info))
            {
                throw new Exception(SR.ConnectionServiceListDbErrorNotConnected(listDatabasesParams.OwnerUri));
            }

            info.TryGetConnection(ConnectionType.Default, out ReliableDataSourceConnection connection);
            IDataSource dataSource = connection.GetUnderlyingConnection();

            return dataSource.GetDatabases(info.ConnectionDetails.ServerName, listDatabasesParams.IncludeDetails.HasTrue());
        }

        public void InitializeService(IProtocolEndpoint serviceHost, IDataSourceConnectionFactory dataSourceConnectionFactory, 
            IConnectedBindingQueue connectedBindingQueue, IConnectionManager connectionManager)
        {
            _serviceHost = serviceHost;
            _dataSourceConnectionFactory = dataSourceConnectionFactory;
            _connectionManager = connectionManager;
            connectedQueues.AddOrUpdate("Default", connectedBindingQueue, (key, old) => connectedBindingQueue);
            LockedDatabaseManager.ConnectionService = this;

            // Register request and event handlers with the Service Host
            serviceHost.SetRequestHandler(ConnectionRequest.Type, HandleConnectRequest);
            serviceHost.SetRequestHandler(CancelConnectRequest.Type, HandleCancelConnectRequest);
            serviceHost.SetRequestHandler(DisconnectRequest.Type, HandleDisconnectRequest);
            serviceHost.SetRequestHandler(ListDatabasesRequest.Type, HandleListDatabasesRequest);
            serviceHost.SetRequestHandler(ChangeDatabaseRequest.Type, HandleChangeDatabaseRequest);
            serviceHost.SetRequestHandler(GetConnectionStringRequest.Type, HandleGetConnectionStringRequest);
            serviceHost.SetRequestHandler(BuildConnectionInfoRequest.Type, HandleBuildConnectionInfoRequest);
        }

        /// <summary> 
        /// Add a new method to be called when the onconnection request is submitted 
        /// </summary> 
        /// <param name="activity"></param> 
        public void RegisterOnConnectionTask(OnConnectionHandler activity)
        {
            onConnectionActivities.Add(activity);
        }

        /// <summary>
        /// Add a new method to be called when the ondisconnect request is submitted
        /// </summary>
        public void RegisterOnDisconnectTask(OnDisconnectHandler activity)
        {
            onDisconnectActivities.Add(activity);
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
                await Task.Run(async () => await RunConnectRequestHandlerTask(connectParams));
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        private async Task RunConnectRequestHandlerTask(ConnectParams connectParams)
        {
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await Connect(connectParams);
                await _serviceHost.SendEvent(ConnectionCompleteNotification.Type, result);
            }
            catch (Exception ex)
            {
                var result = new ConnectionCompleteParams
                {
                    Messages = ex.ToString()
                };
                await _serviceHost.SendEvent(ConnectionCompleteNotification.Type, result);
            }
        }

        /// <summary>
        /// Handle cancel connect requests
        /// </summary>
        protected async Task HandleCancelConnectRequest(
            CancelConnectParams cancelParams,
            RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCancelConnectRequest");

            try
            {
                bool result = CancelConnect(cancelParams);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handle disconnect requests
        /// </summary>
        private async Task HandleDisconnectRequest(DisconnectParams disconnectParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleDisconnectRequest");

            try
            {
                bool result = Disconnect(disconnectParams);
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }

        }

        /// <summary>
        /// Handle requests to list databases on the current server
        /// </summary>
        private async Task HandleListDatabasesRequest(ListDatabasesParams listDatabasesParams, RequestContext<ListDatabasesResponse> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "ListDatabasesRequest");

            try
            {
                var result = await Task.Run(() => ListDatabases(listDatabasesParams));
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handles a request to get a connection string for the provided connection
        /// </summary>
        public async Task HandleGetConnectionStringRequest(
            GetConnectionStringParams connStringParams,
            RequestContext<string> requestContext)
        {
            await Task.Run(async () =>
            {
                string connectionString = string.Empty;
                ConnectionInfo info;
                if (_connectionManager.TryGetValue(connStringParams.OwnerUri, out info))
                {
                    try
                    {
                        if (!connStringParams.IncludePassword)
                        {
                            info.ConnectionDetails.Password = ConnectionService.PasswordPlaceholder;
                        }

                        info.ConnectionDetails.ApplicationName = "ads-connection-string";
                        connectionString = DataSourceFactory.CreateConnectionStringBuilder(DataSourceType.Kusto,
                            info.ConnectionDetails.ServerName, info.ConnectionDetails.DatabaseName).ToString();
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e.ToString());
                    }
                }

                await requestContext.SendResult(connectionString);
            });
        }

        /// <summary>
        /// Handles a request to serialize a connection string
        /// </summary>
        public async Task HandleBuildConnectionInfoRequest(
            string connectionString,
            RequestContext<ConnectionDetails> requestContext)
        {
            await Task.Run(async () =>
            {
                try
                {
                    await requestContext.SendResult(ParseConnectionString(connectionString));
                }
                catch (Exception)
                {
                    // If theres an error in the parse, it means we just can't parse, so we return undefined
                    // rather than an error.
                    await requestContext.SendResult(null);
                }
            });
        }

        public ConnectionDetails ParseConnectionString(string connectionString)
        {
            var builder = DataSourceFactory.CreateConnectionStringBuilder(DataSourceType.Kusto, connectionString);
            return new ConnectionDetails
            {
                ApplicationName = builder.ApplicationNameForTracing,
                AuthenticationType = "AzureMFA",
                DatabaseName = builder.InitialCatalog,
                ServerName = builder.DataSource,
                UserName = builder.UserID,
            };
        }

        /// <summary>
        /// Handles a request to change the database for a connection
        /// </summary>
        private async Task HandleChangeDatabaseRequest(ChangeDatabaseParams changeDatabaseParams, RequestContext<bool> requestContext)
        {
            bool result = await Task.Run(() => result = ChangeConnectionDatabaseContext(changeDatabaseParams.OwnerUri, changeDatabaseParams.NewDatabase, true));
            await requestContext.SendResult(result);
        }

        /// <summary>
        /// Change the database context of a connection.
        /// </summary>
        /// <param name="ownerUri">URI of the owner of the connection</param>
        /// <param name="newDatabaseName">Name of the database to change the connection to</param>
        private bool ChangeConnectionDatabaseContext(string ownerUri, string newDatabaseName, bool force = false)
        {
            if (!_connectionManager.TryGetValue(ownerUri, out ConnectionInfo info))
            {
                return false;
            }
            
            try
            {
                info.ConnectionDetails.DatabaseName = newDatabaseName;

                foreach (string key in info.AllConnectionTypes)
                {
                    ReliableDataSourceConnection conn;
                    info.TryGetConnection(key, out conn);
                    if (conn != null && conn.Database != newDatabaseName)
                    {
                        if (info.IsCloud && force)
                        {
                            conn.Close();
                            conn.Dispose();
                            info.RemoveConnection(key);

                            // create a kusto connection instance
                            ReliableDataSourceConnection connection = info.Factory.CreateDataSourceConnection(info.ConnectionDetails, ownerUri);
                            connection.Open();
                            info.AddConnection(key, connection);
                        }
                        else
                        {
                            conn.ChangeDatabase(newDatabaseName);
                        }
                    }
                }

                // Fire a connection changed event
                ConnectionChangedParams parameters = new ConnectionChangedParams();
                IConnectionSummary summary = info.ConnectionDetails;
                parameters.Connection = summary.Clone();
                parameters.OwnerUri = ownerUri;
                _serviceHost.SendEvent(ConnectionChangedNotification.Type, parameters);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(
                    TraceEventType.Error,
                    $"Exception caught while trying to change database context to [{newDatabaseName}] for OwnerUri [{ownerUri}]. Exception:{e}"
                );
                return false;
            }
        }

        /// <summary>
        /// Invokes the initial on-connect activities if the provided ConnectParams represents the default
        /// connection.
        /// </summary>
        private void InvokeOnConnectionActivities(ConnectionInfo connectionInfo, ConnectParams connectParams)
        {
            if (connectParams.Type != ConnectionType.Default && connectParams.Type != ConnectionType.GeneralConnection)
            {
                return;
            }

            foreach (var activity in this.onConnectionActivities)
            {
                // not awaiting here to allow handlers to run in the background
                activity(connectionInfo);
            }
        }

        /// <summary>
        /// Invokes the final on-disconnect activities if the provided DisconnectParams represents the default
        /// connection or is null - representing that all connections are being disconnected.
        /// </summary>
        private void InvokeOnDisconnectionActivities(ConnectionInfo connectionInfo)
        {
            foreach (var activity in this.onDisconnectActivities)
            {
                activity(connectionInfo.ConnectionDetails, connectionInfo.OwnerUri);
            }
        }

        /// <summary>
        /// Handles the Telemetry events that occur upon disconnect.
        /// </summary>
        /// <param name="info"></param>
        private void HandleDisconnectTelemetry(ConnectionInfo connectionInfo)
        {
            if (_serviceHost != null)
            {
                try
                {
                    // Send a telemetry notification for intellisense performance metrics
                    _serviceHost.SendEvent(TelemetryNotification.Type, new TelemetryParams()
                    {
                        Params = new TelemetryProperties
                        {
                            Properties = new Dictionary<string, string>
                            {
                                { TelemetryPropertyNames.IsAzure, connectionInfo.IsCloud.ToOneOrZeroString() }
                            },
                            EventName = TelemetryEventNames.IntellisenseQuantile,
                            Measures = connectionInfo.IntellisenseMetrics.Quantile
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Write(TraceEventType.Verbose, "Could not send Connection telemetry event " + ex.ToString());
                }
            }
        }

        public static void EnsureConnectionIsOpen(ReliableDataSourceConnection conn, bool forceReopen = false)
        {
            // verify that the connection is open
            if (forceReopen)
            {
                try
                {
                    // close it in case it's in some non-Closed state
                    conn.Close();
                }
                catch
                {
                    // ignore any exceptions thrown from .Close
                    // if the connection is really broken the .Open call will throw
                }
                finally
                {
                    // try to reopen the connection
                    conn.Open();
                }
            }
        }
    }

    public class AzureAccessToken : IRenewableToken
    {
        public DateTimeOffset TokenExpiry { get; set; }
        public string Resource { get; set; }
        public string Tenant { get; set; }
        public string UserId { get; set; }

        private string accessToken;

        public AzureAccessToken(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public string GetAccessToken()
        {
            return this.accessToken;
        }
    }
}
