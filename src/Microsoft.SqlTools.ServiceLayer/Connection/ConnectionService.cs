//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Main class for the Connection Management services
    /// </summary>
    public class ConnectionService
    {
        public const string AdminConnectionPrefix = "ADMIN:";
        private const string SqlAzureEdition = "SQL Azure";

        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static readonly Lazy<ConnectionService> instance 
            = new Lazy<ConnectionService>(() => new ConnectionService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static ConnectionService Instance
        {
            get
            {
                return instance.Value;
            }
        }
        
        /// <summary>
        /// The SQL connection factory object
        /// </summary>
        private ISqlConnectionFactory connectionFactory;

        private DatabaseLocksManager lockedDatabaseManager;

        private readonly Dictionary<string, ConnectionInfo> ownerToConnectionMap = new Dictionary<string, ConnectionInfo>();

        /// <summary>
        /// A map containing all CancellationTokenSource objects that are associated with a given URI/ConnectionType pair. 
        /// Entries in this map correspond to DbConnection instances that are in the process of connecting. 
        /// </summary>
        private readonly ConcurrentDictionary<CancelTokenKey, CancellationTokenSource> cancelTupleToCancellationTokenSourceMap = 
                    new ConcurrentDictionary<CancelTokenKey, CancellationTokenSource>();

        private readonly object cancellationTokenSourceLock = new object();

        private ConcurrentDictionary<string, IConnectedBindingQueue> connectedQueues = new ConcurrentDictionary<string, IConnectedBindingQueue>();

        /// <summary>
        /// Map from script URIs to ConnectionInfo objects
        /// This is internal for testing access only
        /// </summary>
        internal Dictionary<string, ConnectionInfo> OwnerToConnectionMap
        {
            get
            {
                return this.ownerToConnectionMap;
            }
        }

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
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the connection queue
        /// </summary>
        internal IConnectedBindingQueue ConnectionQueue
        {
            get
            {
                return this.GetConnectedQueue("Default");
            }
        }


        /// <summary>
        /// Default constructor should be private since it's a singleton class, but we need a constructor
        /// for use in unit test mocking.
        /// </summary>
        public ConnectionService()
        {
            var defaultQueue = new ConnectedBindingQueue(needsMetadata: false);
            connectedQueues.AddOrUpdate("Default", defaultQueue, (key, old) => defaultQueue);
            this.LockedDatabaseManager.ConnectionService = this;
        }

        /// <summary>
        /// Returns a connection queue for given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IConnectedBindingQueue GetConnectedQueue(string type)
        {
            IConnectedBindingQueue connectedBindingQueue;
            if (connectedQueues.TryGetValue(type, out connectedBindingQueue))
            {
                return connectedBindingQueue;
            }
            return null;
        }

        /// <summary>
        /// Returns all the connection queues
        /// </summary>
        public IEnumerable<IConnectedBindingQueue> ConnectedQueues
        {
            get
            {
                return this.connectedQueues.Values;
            }
        }

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
        public ISqlConnectionFactory ConnectionFactory
        {
            get
            {
                if (this.connectionFactory == null)
                {
                    this.connectionFactory = new SqlConnectionFactory();
                }
                return this.connectionFactory;
            }

            internal set { this.connectionFactory = value; }
        }
       
        /// <summary>
        /// Test constructor that injects dependency interfaces
        /// </summary>
        /// <param name="testFactory"></param>
        public ConnectionService(ISqlConnectionFactory testFactory)
        {
            this.connectionFactory = testFactory;
        }

        // Attempts to link a URI to an actively used connection for this URI
        public virtual bool TryFindConnection(string ownerUri, out ConnectionInfo connectionInfo)
        {
            return this.ownerToConnectionMap.TryGetValue(ownerUri, out connectionInfo);
        }

        /// <summary>
        /// Validates the given ConnectParams object. 
        /// </summary>
        /// <param name="connectionParams">The params to validate</param>
        /// <returns>A ConnectionCompleteParams object upon validation error, 
        /// null upon validation success</returns>
        public ConnectionCompleteParams ValidateConnectParams(ConnectParams connectionParams)
        {
            string paramValidationErrorMessage;
            if (connectionParams == null)
            {
                return new ConnectionCompleteParams
                {
                    Messages = SR.ConnectionServiceConnectErrorNullParams
                };
            }
            if (!connectionParams.IsValid(out paramValidationErrorMessage))
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

        /// <summary>
        /// Open a connection with the specified ConnectParams
        /// </summary>
        public virtual async Task<ConnectionCompleteParams> Connect(ConnectParams connectionParams)
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
            ConnectionInfo connectionInfo;
            bool connectionChanged = false;
            if (!ownerToConnectionMap.TryGetValue(connectionParams.OwnerUri, out connectionInfo))
            {
                connectionInfo = new ConnectionInfo(ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
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
                connectionInfo = new ConnectionInfo(ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
            }

            // Try to open a connection with the given ConnectParams
            ConnectionCompleteParams response = await TryOpenConnection(connectionInfo, connectionParams);
            if (response != null)
            {
                return response;
            }

            // If this is the first connection for this URI, add the ConnectionInfo to the map
            bool addToMap = connectionChanged || !ownerToConnectionMap.ContainsKey(connectionParams.OwnerUri);
            if (addToMap)
            {
                ownerToConnectionMap[connectionParams.OwnerUri] = connectionInfo;
            }

            // Return information about the connected SQL Server instance
            ConnectionCompleteParams completeParams = GetConnectionCompleteParams(connectionParams.Type, connectionInfo);
            // Invoke callback notifications          
            InvokeOnConnectionActivities(connectionInfo, connectionParams);

            TryCloseConnectionTemporaryConnection(connectionParams, connectionInfo);

            return completeParams;
        }

        private void TryCloseConnectionTemporaryConnection(ConnectParams connectionParams, ConnectionInfo connectionInfo)
        {
            try
            {
                if (connectionParams.Purpose == ConnectionType.ObjectExplorer || connectionParams.Purpose == ConnectionType.Dashboard || connectionParams.Purpose == ConnectionType.GeneralConnection)
                {
                    DbConnection connection;
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
                Logger.Write(LogLevel.Normal, "Failed to close temporary connections. error: " + ex.Message);
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

        private bool IsDefaultConnectionType(string connectionType)
        {
            return string.IsNullOrEmpty(connectionType) || ConnectionType.Default.Equals(connectionType, StringComparison.CurrentCultureIgnoreCase);
        }

        private void DisconnectExistingConnectionIfNeeded(ConnectParams connectionParams, ConnectionInfo connectionInfo, bool disconnectAll)
        {
            // Resolve if it is an existing connection
            // Disconnect active connection if the URI is already connected for this connection type
            DbConnection existingConnection;
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
                DbConnection connection;
                connectionInfo.TryGetConnection(connectionType, out connection);

                // Update with the actual database name in connectionInfo and result
                // Doing this here as we know the connection is open - expect to do this only on connecting
                connectionInfo.ConnectionDetails.DatabaseName = connection.Database;
                if (!string.IsNullOrEmpty(connectionInfo.ConnectionDetails.ConnectionString))
                {
                    // If the connection was set up with a connection string, use the connection string to get the details
                    var connectionString = new SqlConnectionStringBuilder(connection.ConnectionString);
                    response.ConnectionSummary = new ConnectionSummary
                    {
                        ServerName = connectionString.DataSource,
                        DatabaseName = connectionString.InitialCatalog,
                        UserName = connectionString.UserID
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

                var reliableConnection = connection as ReliableSqlConnection;
                DbConnection underlyingConnection = reliableConnection != null
                    ? reliableConnection.GetUnderlyingConnection()
                    : connection;

                ReliableConnectionHelper.ServerInfo serverInfo = ReliableConnectionHelper.GetServerVersion(underlyingConnection);
                response.ServerInfo = new ServerInfo
                {
                    ServerMajorVersion = serverInfo.ServerMajorVersion,
                    ServerMinorVersion = serverInfo.ServerMinorVersion,
                    ServerReleaseVersion = serverInfo.ServerReleaseVersion,
                    EngineEditionId = serverInfo.EngineEditionId,
                    ServerVersion = serverInfo.ServerVersion,
                    ServerLevel = serverInfo.ServerLevel,
                    ServerEdition = MapServerEdition(serverInfo),
                    IsCloud = serverInfo.IsCloud,
                    AzureVersion = serverInfo.AzureVersion,
                    OsVersion = serverInfo.OsVersion,
                    MachineName = serverInfo.MachineName
                };
                connectionInfo.IsCloud = serverInfo.IsCloud;
                connectionInfo.MajorVersion = serverInfo.ServerMajorVersion;
                connectionInfo.IsSqlDb = serverInfo.EngineEditionId == (int)DatabaseEngineEdition.SqlDatabase;
                connectionInfo.IsSqlDW = (serverInfo.EngineEditionId == (int)DatabaseEngineEdition.SqlDataWarehouse);
            }
            catch (Exception ex)
            {
                response.Messages = ex.ToString();
            }

            return response;
        }

        private string MapServerEdition(ReliableConnectionHelper.ServerInfo serverInfo)
        {
            string serverEdition = serverInfo.ServerEdition;
            if (string.IsNullOrWhiteSpace(serverEdition))
            {
                return string.Empty;
            }
            if (SqlAzureEdition.Equals(serverEdition, StringComparison.OrdinalIgnoreCase))
            {
                switch(serverInfo.EngineEditionId)
                {
                    case (int) DatabaseEngineEdition.SqlDataWarehouse:
                        serverEdition = SR.AzureSqlDwEdition;
                        break;
                    case (int) DatabaseEngineEdition.SqlStretchDatabase:
                        serverEdition = SR.AzureSqlStretchEdition;
                        break;
                    default:
                        serverEdition =  SR.AzureSqlDbEdition;
                        break;
                }
            }
            return serverEdition;
        }

        /// <summary>
        /// Tries to create and open a connection with the given ConnectParams.
        /// </summary>
        /// <returns>null upon success, a ConnectionCompleteParams detailing the error upon failure</returns>
        private async Task<ConnectionCompleteParams> TryOpenConnection(ConnectionInfo connectionInfo, ConnectParams connectionParams)
        {
            CancellationTokenSource source = null;
            DbConnection connection = null;
            CancelTokenKey cancelKey = new CancelTokenKey { OwnerUri = connectionParams.OwnerUri, Type = connectionParams.Type };
            ConnectionCompleteParams response = new ConnectionCompleteParams { OwnerUri = connectionInfo.OwnerUri, Type = connectionParams.Type };
            bool? currentPooling = connectionInfo.ConnectionDetails.Pooling;

            try
            {
                connectionInfo.ConnectionDetails.Pooling = false;
                // build the connection string from the input parameters
                string connectionString = BuildConnectionString(connectionInfo.ConnectionDetails);

                // create a sql connection instance
                connection = connectionInfo.Factory.CreateSqlConnection(connectionString);
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
        public virtual async Task<DbConnection> GetOrOpenConnection(string ownerUri, string connectionType, bool alwaysPersistSecurity = false)
        {
            Validate.IsNotNullOrEmptyString(nameof(ownerUri), ownerUri);
            Validate.IsNotNullOrEmptyString(nameof(connectionType), connectionType);

            // Try to get the ConnectionInfo, if it exists
            ConnectionInfo connectionInfo;
            if (!ownerToConnectionMap.TryGetValue(ownerUri, out connectionInfo))
            {
                throw new ArgumentOutOfRangeException(SR.ConnectionServiceListDbErrorNotConnected(ownerUri));
            }

            // Make sure a default connection exists
            DbConnection defaultConnection;
            if (!connectionInfo.TryGetConnection(ConnectionType.Default, out defaultConnection))
            {
                throw new InvalidOperationException(SR.ConnectionServiceDbErrorDefaultNotConnected(ownerUri));
            }

            if(IsDedicatedAdminConnection(connectionInfo.ConnectionDetails))
            {
                // Since this is a dedicated connection only 1 is allowed at any time. Return the default connection for use in the requested action
                return defaultConnection;
            }

            // Try to get the DbConnection
            DbConnection connection;
            if (!connectionInfo.TryGetConnection(connectionType, out connection) && ConnectionType.Default != connectionType)
            {
                // If the DbConnection does not exist and is not the default connection, create one.
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
                
                connectionInfo.TryGetConnection(connectionType, out connection);
            }

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
            if (!ownerToConnectionMap.TryGetValue(disconnectParams.OwnerUri, out info))
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
                ownerToConnectionMap.Remove(disconnectParams.OwnerUri);
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
        /// If connectionType is not null, closes the DbConnection with the type given by connectionType.
        /// If connectionType is null, closes all DbConnections.
        /// </summary>
        /// <returns>true if connections were found and attempted to be closed,
        /// false if no connections were found</returns>
        private bool CloseConnections(ConnectionInfo connectionInfo, string connectionType)
        {
            ICollection<DbConnection> connectionsToDisconnect = new List<DbConnection>();
            if (connectionType == null)
            {
                connectionsToDisconnect = connectionInfo.AllConnections;
            }
            else
            {
                // Make sure there is an existing connection of this type
                DbConnection connection;
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

            foreach (DbConnection connection in connectionsToDisconnect)
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
        public ListDatabasesResponse ListDatabases(ListDatabasesParams listDatabasesParams)
        {
            // Verify parameters
            var owner = listDatabasesParams.OwnerUri;
            if (string.IsNullOrEmpty(owner))
            {
                throw new ArgumentException(SR.ConnectionServiceListDbErrorNullOwnerUri);
            }

            // Use the existing connection as a base for the search
            ConnectionInfo info;
            if (!TryFindConnection(owner, out info))
            {
                throw new Exception(SR.ConnectionServiceListDbErrorNotConnected(owner));
            }
            ConnectionDetails connectionDetails = info.ConnectionDetails.Clone();

            // Connect to master and query sys.databases
            connectionDetails.DatabaseName = "master";
            var connection = this.ConnectionFactory.CreateSqlConnection(BuildConnectionString(connectionDetails));
            connection.Open();
            
            List<string> results = new List<string>();
            var systemDatabases = new[] {"master", "model", "msdb", "tempdb"};
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sys.databases ORDER BY name ASC";
                command.CommandTimeout = 15;
                command.CommandType = CommandType.Text; 

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(reader[0].ToString());
                    }
                }
            }

            // Put system databases at the top of the list
            results = 
                results.Where(s => systemDatabases.Any(s.Equals)).Concat(
                results.Where(s => systemDatabases.All(x => !s.Equals(x)))).ToList();

            connection.Close();

            ListDatabasesResponse response = new ListDatabasesResponse();
            response.DatabaseNames = results.ToArray();

            return response;
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Register request and event handlers with the Service Host
            serviceHost.SetRequestHandler(ConnectionRequest.Type, HandleConnectRequest);
            serviceHost.SetRequestHandler(CancelConnectRequest.Type, HandleCancelConnectRequest);
            serviceHost.SetRequestHandler(DisconnectRequest.Type, HandleDisconnectRequest);
            serviceHost.SetRequestHandler(ListDatabasesRequest.Type, HandleListDatabasesRequest);
            serviceHost.SetRequestHandler(ChangeDatabaseRequest.Type, HandleChangeDatabase);
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
        protected async Task HandleConnectRequest(
            ConnectParams connectParams,
            RequestContext<bool> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleConnectRequest");

            try
            {
                RunConnectRequestHandlerTask(connectParams);
                await requestContext.SendResult(true);
            }
            catch
            {
                await requestContext.SendResult(false);
            }
        }

        private void RunConnectRequestHandlerTask(ConnectParams connectParams)
        {
            // create a task to connect asynchronously so that other requests are not blocked in the meantime
            Task.Run(async () => 
            {
                try
                {
                    // result is null if the ConnectParams was successfully validated 
                    ConnectionCompleteParams result = ValidateConnectParams(connectParams);
                    if (result != null)
                    {
                        await ServiceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                        return;
                    }

                    // open connection based on request details
                    result = await Connect(connectParams);
                    await ServiceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                }
                catch (Exception ex)
                {
                    ConnectionCompleteParams result = new ConnectionCompleteParams()
                    {
                        Messages = ex.ToString()
                    };
                    await ServiceHost.SendEvent(ConnectionCompleteNotification.Type, result);
                }
            });
        }

        /// <summary>
        /// Handle cancel connect requests
        /// </summary>
        protected async Task HandleCancelConnectRequest(
            CancelConnectParams cancelParams,
            RequestContext<bool> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleCancelConnectRequest");

            try
            {
                bool result = CancelConnect(cancelParams);
                await requestContext.SendResult(result);
            }
            catch(Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Handle disconnect requests
        /// </summary>
        protected async Task HandleDisconnectRequest(
            DisconnectParams disconnectParams,
            RequestContext<bool> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleDisconnectRequest");

            try
            {
                bool result = Instance.Disconnect(disconnectParams);
                await requestContext.SendResult(result);
            }
            catch(Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }

        }

        /// <summary>
        /// Handle requests to list databases on the current server
        /// </summary>
        protected async Task HandleListDatabasesRequest(
            ListDatabasesParams listDatabasesParams,
            RequestContext<ListDatabasesResponse> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "ListDatabasesRequest");

            try
            {
                ListDatabasesResponse result = ListDatabases(listDatabasesParams);
                await requestContext.SendResult(result);
            }
            catch(Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        /// <summary>
        /// Checks if a ConnectionDetails object represents a DAC connection
        /// </summary>
        /// <param name="connectionDetails"></param>
        public static bool IsDedicatedAdminConnection(ConnectionDetails connectionDetails)
        {
            Validate.IsNotNull(nameof(connectionDetails), connectionDetails);
            SqlConnectionStringBuilder builder = CreateConnectionStringBuilder(connectionDetails);
            string serverName = builder.DataSource;
            return serverName != null && serverName.StartsWith(AdminConnectionPrefix, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Build a connection string from a connection details instance
        /// </summary>
        /// <param name="connectionDetails"></param>
        public static string BuildConnectionString(ConnectionDetails connectionDetails)
        {
            return CreateConnectionStringBuilder(connectionDetails).ToString();
        }

        /// <summary>
        /// Build a connection string builder a connection details instance
        /// </summary>
        /// <param name="connectionDetails"></param>
        public static SqlConnectionStringBuilder CreateConnectionStringBuilder(ConnectionDetails connectionDetails)
        {
            SqlConnectionStringBuilder connectionBuilder;

            // If connectionDetails has a connection string already, use it to initialize the connection builder, then override any provided options.
            // Otherwise use the server name, username, and password from the connection details.
            if (!string.IsNullOrEmpty(connectionDetails.ConnectionString))
            {
                connectionBuilder = new SqlConnectionStringBuilder(connectionDetails.ConnectionString);
            }
            else 
            {
                // add alternate port to data source property if provided
                string dataSource = !connectionDetails.Port.HasValue
                    ? connectionDetails.ServerName
                    : string.Format("{0},{1}", connectionDetails.ServerName, connectionDetails.Port.Value);

                connectionBuilder = new SqlConnectionStringBuilder
                {
                    ["Data Source"] = dataSource,
                    ["User Id"] = connectionDetails.UserName,
                    ["Password"] = connectionDetails.Password
                };
            }

            // Check for any optional parameters
            if (!string.IsNullOrEmpty(connectionDetails.DatabaseName))
            {
                connectionBuilder["Initial Catalog"] = connectionDetails.DatabaseName;
            }
            if (!string.IsNullOrEmpty(connectionDetails.AuthenticationType))
            {
                switch(connectionDetails.AuthenticationType)
                {
                    case "Integrated":
                        connectionBuilder.IntegratedSecurity = true;
                        break;
                    case "SqlLogin":
                        break;
                    default:
                        throw new ArgumentException(SR.ConnectionServiceConnStringInvalidAuthType(connectionDetails.AuthenticationType));
                }
            }
            if (connectionDetails.Encrypt.HasValue)
            {
                connectionBuilder.Encrypt = connectionDetails.Encrypt.Value;
            }
            if (connectionDetails.TrustServerCertificate.HasValue)
            {
                connectionBuilder.TrustServerCertificate = connectionDetails.TrustServerCertificate.Value;
            }
            if (connectionDetails.PersistSecurityInfo.HasValue)
            {
                connectionBuilder.PersistSecurityInfo = connectionDetails.PersistSecurityInfo.Value;
            }
            if (connectionDetails.ConnectTimeout.HasValue)
            {
                connectionBuilder.ConnectTimeout = connectionDetails.ConnectTimeout.Value;
            }
            if (connectionDetails.ConnectRetryCount.HasValue)
            {
                connectionBuilder.ConnectRetryCount = connectionDetails.ConnectRetryCount.Value;
            }
            if (connectionDetails.ConnectRetryInterval.HasValue)
            {
                connectionBuilder.ConnectRetryInterval = connectionDetails.ConnectRetryInterval.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.ApplicationName))
            {
                connectionBuilder.ApplicationName = connectionDetails.ApplicationName;
            }
            if (!string.IsNullOrEmpty(connectionDetails.WorkstationId))
            {
                connectionBuilder.WorkstationID = connectionDetails.WorkstationId;
            }
            if (!string.IsNullOrEmpty(connectionDetails.ApplicationIntent))
            {
                ApplicationIntent intent;
                switch (connectionDetails.ApplicationIntent)
                {
                    case "ReadOnly":
                        intent = ApplicationIntent.ReadOnly;
                        break;
                    case "ReadWrite":
                        intent = ApplicationIntent.ReadWrite;
                        break;
                    default:
                        throw new ArgumentException(SR.ConnectionServiceConnStringInvalidIntent(connectionDetails.ApplicationIntent));
                }
                connectionBuilder.ApplicationIntent = intent;
            }
            if (!string.IsNullOrEmpty(connectionDetails.CurrentLanguage))
            {
                connectionBuilder.CurrentLanguage = connectionDetails.CurrentLanguage;
            }
            if (connectionDetails.Pooling.HasValue)
            {
                connectionBuilder.Pooling = connectionDetails.Pooling.Value;
            }
            if (connectionDetails.MaxPoolSize.HasValue)
            {
                connectionBuilder.MaxPoolSize = connectionDetails.MaxPoolSize.Value;
            }
            if (connectionDetails.MinPoolSize.HasValue)
            {
                connectionBuilder.MinPoolSize = connectionDetails.MinPoolSize.Value;
            }
            if (connectionDetails.LoadBalanceTimeout.HasValue)
            {
                connectionBuilder.LoadBalanceTimeout = connectionDetails.LoadBalanceTimeout.Value;
            }
            if (connectionDetails.Replication.HasValue)
            {
                connectionBuilder.Replication = connectionDetails.Replication.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.AttachDbFilename))
            {
                connectionBuilder.AttachDBFilename = connectionDetails.AttachDbFilename;
            }
            if (!string.IsNullOrEmpty(connectionDetails.FailoverPartner))
            {
                connectionBuilder.FailoverPartner = connectionDetails.FailoverPartner;
            }
            if (connectionDetails.MultiSubnetFailover.HasValue)
            {
                connectionBuilder.MultiSubnetFailover = connectionDetails.MultiSubnetFailover.Value;
            }
            if (connectionDetails.MultipleActiveResultSets.HasValue)
            {
                connectionBuilder.MultipleActiveResultSets = connectionDetails.MultipleActiveResultSets.Value;
            }
            if (connectionDetails.PacketSize.HasValue)
            {
                connectionBuilder.PacketSize = connectionDetails.PacketSize.Value;
            }
            if (!string.IsNullOrEmpty(connectionDetails.TypeSystemVersion))
            {
                connectionBuilder.TypeSystemVersion = connectionDetails.TypeSystemVersion;
            }
            connectionBuilder.Pooling = false;

            return connectionBuilder;
        }

        /// <summary>
        /// Handles a request to change the database for a connection
        /// </summary>
        public async Task HandleChangeDatabase(
            ChangeDatabaseParams changeDatabaseParams,
            RequestContext<bool> requestContext)
        {
            ChangeConnectionDatabaseContext(changeDatabaseParams.OwnerUri, changeDatabaseParams.NewDatabase);
            await requestContext.SendResult(true);
        }

        /// <summary>
        /// Change the database context of a connection.
        /// </summary>
        /// <param name="ownerUri">URI of the owner of the connection</param>
        /// <param name="newDatabaseName">Name of the database to change the connection to</param>
        public void ChangeConnectionDatabaseContext(string ownerUri, string newDatabaseName)
        {
            ConnectionInfo info;
            if (TryFindConnection(ownerUri, out info))
            {
                try
                {
                    foreach (DbConnection connection in info.AllConnections)
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.ChangeDatabase(newDatabaseName);
                        }
                    }

                    info.ConnectionDetails.DatabaseName = newDatabaseName;

                    // Fire a connection changed event
                    ConnectionChangedParams parameters = new ConnectionChangedParams();
                    IConnectionSummary summary = info.ConnectionDetails;
                    parameters.Connection = summary.Clone();
                    parameters.OwnerUri = ownerUri;
                    ServiceHost.SendEvent(ConnectionChangedNotification.Type, parameters);
                }
                catch (Exception e)
                {
                    Logger.Write(
                        LogLevel.Error,
                        string.Format(
                            "Exception caught while trying to change database context to [{0}] for OwnerUri [{1}]. Exception:{2}", 
                            newDatabaseName, 
                            ownerUri, 
                            e.ToString())
                    );
                }
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
            if (ServiceHost != null)
            {
                try
                {
                    // Send a telemetry notification for intellisense performance metrics
                    ServiceHost.SendEvent(TelemetryNotification.Type, new TelemetryParams()
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
                    Logger.Write(LogLevel.Verbose, "Could not send Connection telemetry event " + ex.ToString());
                }
            }
        }

        /// <summary>
        /// Create and open a new SqlConnection from a ConnectionInfo object
        /// Note: we need to audit all uses of this method to determine why we're
        /// bypassing normal ConnectionService connection management
        /// </summary>
        internal static SqlConnection OpenSqlConnection(ConnectionInfo connInfo, string featureName = null)
        {
            try
            {
                // capture original values
                int? originalTimeout = connInfo.ConnectionDetails.ConnectTimeout;
                bool? originalPersistSecurityInfo = connInfo.ConnectionDetails.PersistSecurityInfo;
                bool? originalPooling = connInfo.ConnectionDetails.Pooling;

                // increase the connection timeout to at least 30 seconds and and build connection string
                connInfo.ConnectionDetails.ConnectTimeout = Math.Max(30, originalTimeout ?? 0);
                // enable PersistSecurityInfo to handle issues in SMO where the connection context is lost in reconnections
                connInfo.ConnectionDetails.PersistSecurityInfo = true;
                // turn off connection pool to avoid hold locks on server resources after calling SqlConnection Close method
                connInfo.ConnectionDetails.Pooling = false;
                connInfo.ConnectionDetails.ApplicationName = GetApplicationNameWithFeature(connInfo.ConnectionDetails.ApplicationName, featureName);

                // generate connection string
                string connectionString = ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);

                // restore original values
                connInfo.ConnectionDetails.ConnectTimeout = originalTimeout;
                connInfo.ConnectionDetails.PersistSecurityInfo = originalPersistSecurityInfo;
                connInfo.ConnectionDetails.Pooling = originalPooling;

                // open a dedicated binding server connection
                SqlConnection sqlConn = new SqlConnection(connectionString);
                sqlConn.Open();
                return sqlConn;
            }
            catch (Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, 
                    "Failed opening a SqlConnection: error:{0} inner:{1} stacktrace:{2}",
                    ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty, ex.StackTrace);
                Logger.Write(LogLevel.Error, error);
            }
            
            return null;
        }
    }
}
