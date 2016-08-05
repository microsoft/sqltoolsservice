//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.EditorServices.Utility;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    public class ConnectionInfo
    {
        public ConnectionInfo(ISqlConnectionFactory factory, string ownerUri, ConnectionDetails details)
        {
            Factory = factory;
            OwnerUri = ownerUri;
            ConnectionDetails = details;
            ConnectionId = Guid.NewGuid();
        }

        /// <summary>
        /// Unique Id, helpful to identify a connection info object
        /// </summary>
        public Guid ConnectionId { get; private set; }

        public string OwnerUri { get; private set; }

        public ISqlConnectionFactory Factory {get; private set;}

        public ConnectionDetails ConnectionDetails { get; private set; }
        
        public DbConnection SqlConnection { get; private set; }

        public void OpenConnection()
        {
            // build the connection string from the input parameters
            string connectionString = ConnectionService.BuildConnectionString(ConnectionDetails);

            // create a sql connection instance
            SqlConnection = Factory.CreateSqlConnection(connectionString);
            SqlConnection.Open();
        }
    }

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
        /// List of onconnection handlers
        /// </summary>
        private readonly List<OnConnectionHandler> onConnectionActivities = new List<OnConnectionHandler>();

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
        
        private static ConnectionSummary CopySummary(ConnectionSummary summary)
        {
            return new ConnectionSummary()
            {
                ServerName = summary.ServerName,
                DatabaseName = summary.DatabaseName,
                UserName = summary.UserName
            };
        }

        /// <summary>
        /// Open a connection with the specified connection details
        /// </summary>
        /// <param name="connectionParams"></param>
        public ConnectResponse Connect(ConnectParams connectionParams)
        {
            // Validate parameters
            if(connectionParams == null || !connectionParams.IsValid())
            {
                return new ConnectResponse()
                {
                    Messages = "Error: Invalid connection parameters provided."
                };
            }

            ConnectionInfo connectionInfo;
            if (ownerToConnectionMap.TryGetValue(connectionParams.OwnerUri, out connectionInfo) )
            {
                // TODO disconnect
            }
            connectionInfo = new ConnectionInfo(ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);

            // try to connect
            var response = new ConnectResponse();
            try
            {
                connectionInfo.OpenConnection();
            }
            catch(Exception ex)
            {
                response.Messages = ex.Message;
                return response;
            }

            ownerToConnectionMap[connectionParams.OwnerUri] = connectionInfo;

            // invoke callback notifications
            foreach (var activity in this.onConnectionActivities)
            {
                activity(connectionInfo);
            }

            // return the connection result
            response.ConnectionId = connectionInfo.ConnectionId.ToString();
            return response;
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            // Register request and event handlers with the Service Host
            serviceHost.SetRequestHandler(ConnectionRequest.Type, HandleConnectRequest);

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
                await requestContext.SendError(ex.Message);
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
            connectionBuilder["Integrated Security"] = false;
            connectionBuilder["User Id"] = connectionDetails.UserName;
            connectionBuilder["Password"] = connectionDetails.Password;
            connectionBuilder["Initial Catalog"] = connectionDetails.DatabaseName;
            return connectionBuilder.ToString();
        }
    }
}
