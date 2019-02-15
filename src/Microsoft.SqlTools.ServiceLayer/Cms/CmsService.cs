//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Cms.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Cms
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    public class CmsService
    {
        private static object lockObject = new object();
        private static ConnectionService connectionService = null;
        private static RegisteredServersStore registerdServerStore = null;
        private static readonly Lazy<CmsService> instance = new Lazy<CmsService>(() => new CmsService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static CmsService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(CreateCentralManagementServerRequest.Type, this.HandleCreateCentralManagementServerRequest);
            serviceHost.SetRequestHandler(GetRegisteredServerRequest.Type, this.HandleListRegisteredServersRequest);
            serviceHost.SetRequestHandler(AddRegisteredServerRequest.Type, this.HandleAddRegisteredServerRequest);
            serviceHost.SetRequestHandler(RemoveRegisteredServerRequest.Type, this.HandleRemoveRegisteredServerRequest);
        }

        public async Task HandleCreateCentralManagementServerRequest(CreateCentralManagementServerParams createCmsParams, RequestContext<RegisteredServersResult> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCreateCentralManagementServerRequest");
            try
            {
                RegisteredServersResult result = await CreateCMSTask(createCmsParams);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleRemoveRegisteredServerRequest(RemoveRegisteredServerParams removeServerParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleRemoveServerRequest");
            try
            {
                bool result = RemoveRegisteredServersTask(removeServerParams);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles create servers request
        /// </summary>
        /// <returns></returns>
        public async Task HandleAddRegisteredServerRequest(AddRegisteredServerParams cmsCreateParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleAddRegisteredServerRequest");
            try
            {
                bool result = AddRegisteredServerTask(cmsCreateParams);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles get servers request
        /// </summary>
        /// <returns></returns>
        public async Task HandleListRegisteredServersRequest(ConnectParams connectionParams, RequestContext<RegisteredServersResult> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleListRegisteredServersRequest");
            try
            {
                RegisteredServersResult result = ListRegisteredServersTask(connectionParams);
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        private async Task<RegisteredServersResult> CreateCMSTask(CreateCentralManagementServerParams cmsCreateParams)
        {
            //Validate params and connect
            ServerConnection conn = await ValidateAndCreateConnection(cmsCreateParams.ConnectParams);

            // Get Current Reg Servers
            RegisteredServersStore store = new RegisteredServersStore(conn);
            ServerGroup parentGroup = store.DatabaseEngineServerGroup; //TODO Do we need other types like Integrated Service and Analysis Service
            RegisteredServerCollection servers = parentGroup.RegisteredServers;

            // Convert to appropriate variables and return
            var serverResults = new List<RegisteredServerResult>();
            foreach (var s in servers)
            {
                serverResults.Add(new RegisteredServerResult
                {
                    Name = s.Name,
                    ServerName = s.ServerName ?? cmsCreateParams.ConnectParams.Connection.ServerName,
                    Description = s.Description,
                    connectionDetails = ConnectionServiceInstance.ParseConnectionString(s.ConnectionString)
                });
            }

            RegisteredServersResult result = new RegisteredServersResult() { RegisteredServersList = serverResults };
            return result;
        }
        
        private bool AddRegisteredServerTask(AddRegisteredServerParams cmsCreateParams)
        {
            try
            {
                ServerConnection serverConn = ValidateAndCreateConnection(cmsCreateParams.ParentOwnerUri);
                if (serverConn != null)
                {
                    // Get Current Reg Servers
                    RegisteredServersStore store = new RegisteredServersStore(serverConn);
                    ServerGroup parentGroup = store.DatabaseEngineServerGroup; //TODO Do we need other types like Integrated Service and Analysis Service
                    RegisteredServerCollection servers = parentGroup.RegisteredServers;

                    // Add the new server (intentioanlly not cheching existence to reuse the exception message)
                    RegisteredServer regServer = new RegisteredServer(parentGroup, cmsCreateParams.RegisteredServerName);
                    if (cmsCreateParams.RegServerConnectionDetails != null)
                    {
                        regServer.ServerName = cmsCreateParams.RegServerConnectionDetails.ServerName;
                        regServer.Description = cmsCreateParams.RegisterdServerDescription;
                        regServer.ConnectionString = ConnectionService.CreateConnectionStringBuilder(cmsCreateParams.RegServerConnectionDetails).ToString();
                    }
                    regServer.Description = cmsCreateParams.RegisterdServerDescription;
                    regServer.Create();

                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                Logger.Write(TraceEventType.Error, "Could not create Reg Server {0}");
                throw;
            }

        }

        private RegisteredServersResult ListRegisteredServersTask(ConnectParams connectionParams)
        {
            //Validate and create connection
            ServerConnection serverConn = ValidateAndCreateConnection(connectionParams.OwnerUri);
           
            if (serverConn != null)
            {
                // Get registered Servers
                RegisteredServersStore store = new RegisteredServersStore(serverConn);
                ServerGroup parentGroup = store.DatabaseEngineServerGroup;  //TODO Do we need other types like Integrated Service and Analysis Service
                RegisteredServerCollection servers = parentGroup.RegisteredServers;

                // Convert to appropriate variables and return
                var serverResults = new List<RegisteredServerResult>();
                foreach (var s in servers)
                {
                    serverResults.Add(new RegisteredServerResult
                    {
                        Name = s.Name,
                        ServerName = s.ServerName ?? connectionParams.Connection.ServerName,
                        Description = s.Description,
                        connectionDetails = ConnectionServiceInstance.ParseConnectionString(s.ConnectionString)
                    });
                }
                RegisteredServersResult result = new RegisteredServersResult() { RegisteredServersList = serverResults };
                return result;
            }
            return null;
        }

        private bool RemoveRegisteredServersTask(RemoveRegisteredServerParams removeServerParams)
        {
            // Validate and Connect
            ServerConnection serverConn = ValidateAndCreateConnection(removeServerParams.ParentOwnerUri);
            if (serverConn != null)
            {
                // Get list of registered Servers
                RegisteredServersStore store = new RegisteredServersStore(serverConn);
                ServerGroup parentGroup = store.DatabaseEngineServerGroup;  //TODO Do we need other types like Integrated Service and Analysis Service

                // since duplicates are not allowed
                RegisteredServer regServ = parentGroup.RegisteredServers.OfType<RegisteredServer>().FirstOrDefault(r => r.Name == removeServerParams.RegisteredServerName);
                regServ.Drop();
                return true;
            }
            return false;
        }

        private async Task<ServerConnection> ValidateAndCreateConnection(ConnectParams connectionParams)
        {
            // Validate Parameters and Create Connection
            ConnectionCompleteParams connectionCompleteParams = await ConnectionServiceInstance.Connect(connectionParams);
            if (!string.IsNullOrEmpty(connectionCompleteParams.Messages))
            {
                throw new Exception(connectionCompleteParams.Messages);
            }

            // Get Connection
            ConnectionInfo connectionInfo = ConnectionServiceInstance.OwnerToConnectionMap[connectionParams.OwnerUri];
            ServerConnection serverConn = ConnectionService.OpenServerConnection(connectionInfo);

            return serverConn;
        }

        private ServerConnection ValidateAndCreateConnection(string ownerUri)
        {
            ServerConnection serverConn = null;
            if (ownerUri != null)
            {
                ConnectionInfo connInfo = null;
                if (ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo))
                {
                    serverConn = ConnectionService.OpenServerConnection(connInfo);                    
                }
            }
            return serverConn;
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
        /// Internal for testing purposes only
        /// </summary>
        internal static RegisteredServersStore ServerStore
        {
            get
            {
                if (registerdServerStore == null)
                {
                    registerdServerStore = RegisteredServersStore.LocalFileStore;
                }
                return registerdServerStore;
            }
            set
            {
                lock (lockObject)
                {
                    registerdServerStore = value;
                }
            }
        }
    }
}

