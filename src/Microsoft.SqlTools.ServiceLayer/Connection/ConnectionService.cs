//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
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
        private static Lazy<ConnectionService> instance 
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
           
        private Dictionary<string, ConnectionInfo> ownerToConnectionMap = new Dictionary<string, ConnectionInfo>();

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
        /// Default constructor is private since it's a singleton class
        /// </summary>
        private ConnectionService()
        {
        }

        /// <summary>
        /// Callback for onconnection handler
        /// </summary>
        /// <param name="sqlConnection"></param>
        public delegate Task OnConnectionHandler(ConnectionInfo info);

        /// <summary>
        // Callback for ondisconnect handler
        /// </summary>
        public delegate Task OnDisconnectHandler(ConnectionSummary summary);

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
        public bool TryFindConnection(string ownerUri, out ConnectionInfo connectionInfo)
        {
            return this.ownerToConnectionMap.TryGetValue(ownerUri, out connectionInfo);
        }

        /// <summary>
        /// Open a connection with the specified connection details
        /// </summary>
        /// <param name="connectionParams"></param>
        public ConnectResponse Connect(ConnectParams connectionParams)
        {
            // Validate parameters
            string paramValidationErrorMessage;
            if (connectionParams == null)
            {
                return new ConnectResponse
                {
                    Messages = SR.ConnectionServiceErrorNullParams
                };
            }
            if (!connectionParams.IsValid(out paramValidationErrorMessage))
            {
                return new ConnectResponse
                {
                    Messages = paramValidationErrorMessage
                };
            }

            // Resolve if it is an existing connection
            // Disconnect active connection if the URI is already connected
            ConnectionInfo connectionInfo;
            if (ownerToConnectionMap.TryGetValue(connectionParams.OwnerUri, out connectionInfo) )
            {
                var disconnectParams = new DisconnectParams()
                {
                    OwnerUri = connectionParams.OwnerUri
                };
                Disconnect(disconnectParams);
            }
            connectionInfo = new ConnectionInfo(ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);

            // try to connect
            var response = new ConnectResponse();
            try
            {
                // build the connection string from the input parameters
                string connectionString = ConnectionService.BuildConnectionString(connectionInfo.ConnectionDetails);

                // create a sql connection instance
                connectionInfo.SqlConnection = connectionInfo.Factory.CreateSqlConnection(connectionString);
                connectionInfo.SqlConnection.Open();
            }
            catch(Exception ex)
            {
                response.Messages = ex.ToString();
                return response;
            }

            ownerToConnectionMap[connectionParams.OwnerUri] = connectionInfo;

            // Update with the actual database name in connectionInfo and result
            // Doing this here as we know the connection is open - expect to do this only on connecting
            connectionInfo.ConnectionDetails.DatabaseName = connectionInfo.SqlConnection.Database;
            response.ConnectionSummary = new ConnectionSummary()
            {
                ServerName = connectionInfo.ConnectionDetails.ServerName,
                DatabaseName = connectionInfo.ConnectionDetails.DatabaseName,
                UserName = connectionInfo.ConnectionDetails.UserName,
            };

            // invoke callback notifications
            foreach (var activity in this.onConnectionActivities)
            {
                activity(connectionInfo);
            }

            // return the connection result
            response.ConnectionId = connectionInfo.ConnectionId.ToString();
            return response;
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

            // Lookup the connection owned by the URI
            ConnectionInfo info;
            if (!ownerToConnectionMap.TryGetValue(disconnectParams.OwnerUri, out info))
            {
                return false;
            }

            // Close the connection            
            info.SqlConnection.Close();

            // Remove URI mapping
            ownerToConnectionMap.Remove(disconnectParams.OwnerUri);

            // Invoke callback notifications
            foreach (var activity in this.onDisconnectActivities)
            {
                activity(info.ConnectionDetails);
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
            
            DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.databases";
            command.CommandTimeout = 15;
            command.CommandType = CommandType.Text;
            var reader = command.ExecuteReader();

            List<string> results = new List<string>();
            while (reader.Read())
            {
                results.Add(reader[0].ToString());
            }

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
            RequestContext<ConnectResponse> requestContext)
        {
            Logger.Write(LogLevel.Verbose, "HandleConnectRequest");

            try
            {
                // open connection base on request details
                ConnectResponse result = ConnectionService.Instance.Connect(connectParams);
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
                bool result = ConnectionService.Instance.Disconnect(disconnectParams);
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
                ListDatabasesResponse result = ConnectionService.Instance.ListDatabases(listDatabasesParams);
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
            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder();
            connectionBuilder["Data Source"] = connectionDetails.ServerName;
            connectionBuilder["User Id"] = connectionDetails.UserName;
            connectionBuilder["Password"] = connectionDetails.Password;

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
                    if (info.SqlConnection.State == ConnectionState.Open)
                    {
                        info.SqlConnection.ChangeDatabase(newDatabaseName);
                    }
                    info.ConnectionDetails.DatabaseName = newDatabaseName;

                    // Fire a connection changed event
                    ConnectionChangedParams parameters = new ConnectionChangedParams();
                    ConnectionSummary summary = (ConnectionSummary)(info.ConnectionDetails);
                    parameters.Connection = summary.Clone();
                    parameters.OwnerUri = ownerUri;
                    ServiceHost.SendEvent(ConnectionChangedNotification.Type, parameters);
                }
                catch (Exception e)
                {
                    Logger.Write(
                        LogLevel.Error,
                        // TODO: Do log messages need to localized?
                        string.Format(
                            "Exception caught while trying to change database context to [{0}] for OwnerUri [{1}]. Exception:{2}", 
                            newDatabaseName, 
                            ownerUri, 
                            e.ToString())
                    );
                }
            }
        }
    }
}
