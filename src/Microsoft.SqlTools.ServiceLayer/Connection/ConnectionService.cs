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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Workspace;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Main class for the Connection Management services
    /// </summary>
    public class ConnectionService
    {
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
           
        private readonly Dictionary<string, ConnectionInfo> ownerToConnectionMap = new Dictionary<string, ConnectionInfo>();

        /// <summary>
        /// A map containing all CancellationTokenSource objects that are associated with a given URI/ConnectionType pair. 
        /// Entries in this map correspond to DbConnection instances that are in the process of connecting. 
        /// Tuple is used here becasue of it's predictable hash code behavior. 
        /// </summary>
        private readonly ConcurrentDictionary<Tuple<string, ConnectionType>, CancellationTokenSource> CancelTupleToCancellationTokenSourceMap = 
                    new ConcurrentDictionary<Tuple<string, ConnectionType>, CancellationTokenSource>();

        private readonly object cancellationTokenSourceLock = new object();

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
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor should be private since it's a singleton class, but we need a constructor
        /// for use in unit test mocking.
        /// </summary>
        public ConnectionService()
        {
        }

        /// <summary>
        /// Callback for onconnection handler
        /// </summary>
        /// <param name="sqlConnection"></param>
        public delegate Task OnConnectionHandler(ConnectionInfo info);

        /// <summary>
        /// Callback for ondisconnect handler
        /// </summary>
        public delegate Task OnDisconnectHandler(ConnectionSummary summary, string ownerUri);

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
        public async Task<ConnectionCompleteParams> Connect(ConnectParams connectionParams)
        {
            // Validate parameters
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

            // If there is no ConnectionInfo in the map, create a new ConnectionInfo, 
            // but wait until later when we are connected to add it to the map.
            ConnectionInfo connectionInfo;
            if (!ownerToConnectionMap.TryGetValue(connectionParams.OwnerUri, out connectionInfo))
            {
                connectionInfo = new ConnectionInfo(ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
            }

            // Resolve if it is an existing connection
            // Disconnect active connection if the URI is already connected for this connection type
            DbConnection existingConnection;
            if (connectionInfo.ConnectionTypeToConnectionMap.TryGetValue(connectionParams.Type, out existingConnection))
            {
                var disconnectParams = new DisconnectParams()
                {
                    OwnerUri = connectionParams.OwnerUri, 
                    Type = connectionParams.Type
                };
                Disconnect(disconnectParams);
            }

            // try to connect
            DbConnection connection;
            CancellationTokenSource source = null;
            ConnectionCompleteParams response = new ConnectionCompleteParams
            {
                OwnerUri = connectionInfo.OwnerUri,
                Type = connectionParams.Type
            };
            Tuple<string, ConnectionType> cancelKey = new Tuple<string, ConnectionType>
            (
                connectionParams.OwnerUri,
                connectionParams.Type
            );

            try
            {
                // build the connection string from the input parameters
                string connectionString = BuildConnectionString(connectionInfo.ConnectionDetails);

                // create a sql connection instance
                connection = connectionInfo.Factory.CreateSqlConnection(connectionString);
                connectionInfo.ConnectionTypeToConnectionMap[connectionParams.Type] = connection;

                // Add a cancellation token source so that the connection OpenAsync() can be cancelled
                using (source = new CancellationTokenSource())
                {
                    // Locking here to perform two operations as one atomic operation
                    lock (cancellationTokenSourceLock)
                    {
                        // If the URI is currently connecting from a different request, cancel it before we try to connect
                        CancellationTokenSource currentSource;
                        if (CancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out currentSource))
                        {
                            currentSource.Cancel();
                        }
                        CancelTupleToCancellationTokenSourceMap[cancelKey] = source;
                    }

                    // Create a task to handle cancellation requests
                    var cancellationTask = Task.Run(() =>
                    {
                        source.Token.WaitHandle.WaitOne();
                        try
                        {
                            source.Token.ThrowIfCancellationRequested();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Ignore
                        }
                    });

                    var openTask = Task.Run(async () =>
                    {
                        await connection.OpenAsync(source.Token);
                    });
                    
                    // Open the connection
                    await Task.WhenAny(openTask, cancellationTask).Unwrap();
                    source.Cancel();
                }
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
                    if (CancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out sourceValue) && sourceValue == source)
                    {
                        CancelTupleToCancellationTokenSourceMap.TryRemove(cancelKey, out sourceValue);
                    }
                }
            }

            // If this is the first connection for this URI, add the ConnectionInfo to the map
            if (!ownerToConnectionMap.ContainsKey(connectionParams.OwnerUri))
            {
                ownerToConnectionMap[connectionParams.OwnerUri] = connectionInfo;
            }

            // Update with the actual database name in connectionInfo and result
            // Doing this here as we know the connection is open - expect to do this only on connecting
            connectionInfo.ConnectionDetails.DatabaseName = connection.Database;
            response.ConnectionSummary = new ConnectionSummary
            {
                ServerName = connectionInfo.ConnectionDetails.ServerName,
                DatabaseName = connectionInfo.ConnectionDetails.DatabaseName,
                UserName = connectionInfo.ConnectionDetails.UserName,
            };

            // TODO do these activities depend on the type of connection?
            // Should they only get run when the Default connection is established?

            // invoke callback notifications
            InvokeOnConnectionActivities(connectionInfo);

            // try to get information about the connected SQL Server instance
            try
            {
                var reliableConnection = connection as ReliableSqlConnection;
                DbConnection underlyingConnection = reliableConnection != null ? reliableConnection.GetUnderlyingConnection() : connection;
                
                ReliableConnectionHelper.ServerInfo serverInfo = ReliableConnectionHelper.GetServerVersion(underlyingConnection);
                response.ServerInfo = new ServerInfo
                {
                    ServerMajorVersion = serverInfo.ServerMajorVersion,
                    ServerMinorVersion = serverInfo.ServerMinorVersion,
                    ServerReleaseVersion = serverInfo.ServerReleaseVersion,
                    EngineEditionId = serverInfo.EngineEditionId,
                    ServerVersion = serverInfo.ServerVersion,
                    ServerLevel = serverInfo.ServerLevel,
                    ServerEdition = serverInfo.ServerEdition,
                    IsCloud = serverInfo.IsCloud,
                    AzureVersion = serverInfo.AzureVersion,
                    OsVersion = serverInfo.OsVersion
                };
                connectionInfo.IsAzure = serverInfo.IsCloud;
            }
            catch(Exception ex)
            {
                response.Messages = ex.ToString();
            }

            // return the connection result
            response.ConnectionId = connectionInfo.ConnectionId.ToString();
            return response;
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

            Tuple<string, ConnectionType> cancelKey = new Tuple<string, ConnectionType>(
                cancelParams.OwnerUri,
                cancelParams.Type);

            // Cancel any current connection attempts for this URI
            CancellationTokenSource source;
            if (CancelTupleToCancellationTokenSourceMap.TryGetValue(cancelKey, out source))
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
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Close a connection with the specified connection details.
        /// </summary>
        public bool Disconnect(DisconnectParams disconnectParams)
        {
            // Validate parameters
            if (disconnectParams == null || string.IsNullOrEmpty(disconnectParams.OwnerUri))
            {
                return false;
            }

            ConnectionInfo info = null;

            // If we are disconnecting all existing connections for the provided URI
            if (disconnectParams.Type == null)
            {
                // Cancel all pending connections
                foreach (KeyValuePair<Tuple<string, ConnectionType>, CancellationTokenSource> entry in CancelTupleToCancellationTokenSourceMap)
                {
                    string entryConnectionUri = entry.Key.Item1;
                    ConnectionType entryConnectionType = entry.Key.Item2;
                    if (disconnectParams.OwnerUri.Equals(entryConnectionUri))
                    {
                        CancelConnect(new CancelConnectParams() { OwnerUri = disconnectParams.OwnerUri, Type = entryConnectionType });
                    }
                }

                // Lookup the connection owned by the URI
                if (!ownerToConnectionMap.TryGetValue(disconnectParams.OwnerUri, out info))
                {
                    return false;
                }

                // Close all open connections
                bool closeFailed = false;
                List<ConnectionType> connectionsToRemove = new List<ConnectionType>();

                foreach (KeyValuePair<ConnectionType, DbConnection> entry in info.ConnectionTypeToConnectionMap)
                {
                    try
                    { 
                        entry.Value.Close();
                        connectionsToRemove.Add(entry.Key);
                    }
                    catch (System.Data.Common.DbException e)
                    {
                        closeFailed = true;
                    }
                }

                // If there we no errors calling Close(), simply clear the map and continue
                if (!closeFailed)
                {
                    info.ConnectionTypeToConnectionMap.Clear();
                    ownerToConnectionMap.Remove(disconnectParams.OwnerUri);
                }
                // If there were errors, we will remove only the connections that successfully closed
                else
                {
                    foreach(ConnectionType type in connectionsToRemove)
                    {
                        info.ConnectionTypeToConnectionMap.Remove(type);
                    }

                    return false;
                }

            }
            // If we are disconnecting only a single type of connection
            else
            {
                // Get non-nullable ConnectionType
                ConnectionType connectionType = (ConnectionType)disconnectParams.Type;

                // Cancel if we are in the middle of connecting
                if (CancelConnect(new CancelConnectParams() { OwnerUri = disconnectParams.OwnerUri, Type = connectionType }))
                {
                    return false;
                }

                // Lookup the connection owned by the URI
                if (!ownerToConnectionMap.TryGetValue(disconnectParams.OwnerUri, out info))
                {
                    return false;
                }

                // Make sure there is an existing connection of this type
                DbConnection connection;
                if (!info.ConnectionTypeToConnectionMap.TryGetValue(connectionType, out connection))
                {
                    return false;
                }

                // Try to close the connection
                try
                {
                    connection.Close();
                }
                catch (System.Data.Common.DbException e)
                {
                    return false;
                }

                // If successful, remove Type mapping 
                info.ConnectionTypeToConnectionMap.Remove(connectionType);

                // Remove URI mapping if there are no more connections in the type ConnectionTypeToConnectionMap
                if (info.ConnectionTypeToConnectionMap.Count == 0)
                {
                    ownerToConnectionMap.Remove(disconnectParams.OwnerUri);
                }
            }

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
                            { "IsAzure", info.IsAzure ? "1" : "0" }
                        },
                            EventName = TelemetryEventNames.IntellisenseQuantile,
                            Measures = info.IntellisenseMetrics.Quantile
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Write(LogLevel.Verbose, "Could not send Connection telemetry event " + ex.ToString());
                }
            }

            // TODO do these activities depend on the type of connection?
            // Should they only get run when all connections for a given URI are disconnected?

            // Invoke callback notifications
            foreach (var activity in this.onDisconnectActivities)
            {
                activity(info.ConnectionDetails, disconnectParams.OwnerUri);
            }

            // Success
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

            // Register the configuration update handler
            WorkspaceService<SqlToolsSettings>.Instance.RegisterConfigChangeCallback(HandleDidChangeConfigurationNotification);
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
        /// <param name="connectionDetails"></param>
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
                    result = await Instance.Connect(connectParams);
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
                bool result = Instance.CancelConnect(cancelParams);
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
                ListDatabasesResponse result = Instance.ListDatabases(listDatabasesParams);
                await requestContext.SendResult(result);
            }
            catch(Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
        
        public Task HandleDidChangeConfigurationNotification(
            SqlToolsSettings newSettings, 
            SqlToolsSettings oldSettings, 
            EventContext eventContext)
        {
            return Task.FromResult(true);
        }
        
        /// <summary>
        /// Build a connection string from a connection details instance
        /// </summary>
        /// <param name="connectionDetails"></param>
        public static string BuildConnectionString(ConnectionDetails connectionDetails)
        {
            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder
            {
                ["Data Source"] = connectionDetails.ServerName,
                ["User Id"] = connectionDetails.UserName,
                ["Password"] = connectionDetails.Password
            };

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

            return connectionBuilder.ToString();
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
                    foreach (KeyValuePair<ConnectionType, DbConnection> entry in info.ConnectionTypeToConnectionMap)
                    {
                        DbConnection connection = entry.Value;

                        if (connection.State == ConnectionState.Open)
                        {
                            connection.ChangeDatabase(newDatabaseName);
                        }
                    }

                    info.ConnectionDetails.DatabaseName = newDatabaseName;

                    // Fire a connection changed event
                    ConnectionChangedParams parameters = new ConnectionChangedParams();
                    ConnectionSummary summary = info.ConnectionDetails;
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

        private void InvokeOnConnectionActivities(ConnectionInfo connectionInfo)
        {
            foreach (var activity in this.onConnectionActivities)
            {
                // not awaiting here to allow handlers to run in the background
                activity(connectionInfo);
            }
        }
    }
}
