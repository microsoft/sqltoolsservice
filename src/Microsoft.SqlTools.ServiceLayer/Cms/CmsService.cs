//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.SqlServer.Management.Sdk.Sfc;
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
    /// Main class for CmsService
    /// </summary>
    public class CmsService
    {
        private static ConnectionService connectionService = null;
        private static readonly Lazy<CmsService> instance = new Lazy<CmsService>(() => new CmsService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static CmsService Instance
        {
            get { return instance.Value; }
        }

        #region Public methods

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(CreateCentralManagementServerRequest.Type, this.HandleCreateCentralManagementServerRequest);
            serviceHost.SetRequestHandler(ListRegisteredServersRequest.Type, this.HandleListRegisteredServersRequest);
            serviceHost.SetRequestHandler(AddRegisteredServerRequest.Type, this.HandleAddRegisteredServerRequest);
            serviceHost.SetRequestHandler(RemoveRegisteredServerRequest.Type, this.HandleRemoveRegisteredServerRequest);
            serviceHost.SetRequestHandler(AddServerGroupRequest.Type, this.HandleAddServerGroupRequest);
            serviceHost.SetRequestHandler(RemoveServerGroupRequest.Type, this.HandleRemoveServerGroupRequest);
        }

        public async Task HandleCreateCentralManagementServerRequest(CreateCentralManagementServerParams createCmsParams, RequestContext<ListRegisteredServersResult> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleCreateCentralManagementServerRequest");
            try
            {
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        //Validate params and connect
                        ServerConnection conn = await ValidateAndCreateConnection(createCmsParams.ConnectParams);

                        // Get Current Reg Servers on CMS
                        RegisteredServersStore store = new RegisteredServersStore(conn);
                        ServerGroup parentGroup = store.DatabaseEngineServerGroup;                   
                        ListRegisteredServersResult result = GetChildrenfromParentGroup(parentGroup);
                        if (result != null)
                        {
                            await requestContext.SendResult(result);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Exception related to connection/creation will only be caught here. Note that the outer catch will not catch them
                        await requestContext.SendError(ex);
                    }
                });
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        public async Task HandleAddRegisteredServerRequest(AddRegisteredServerParams cmsCreateParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleAddRegisteredServerRequest");
            try
            {
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        ServerConnection serverConn = ValidateAndCreateConnection(cmsCreateParams.ParentOwnerUri);
                        if (serverConn != null)
                        {
                            // Get Current Reg Servers
                            RegisteredServersStore store = new RegisteredServersStore(serverConn);
                            ServerGroup parentGroup = NavigateToServerGroup(store, cmsCreateParams.RelativePath);
                            RegisteredServerCollection servers = parentGroup.RegisteredServers;

                            // Add the new server (intentionally not cheching existence to reuse the exception message)
                            RegisteredServer registeredServer = new RegisteredServer(parentGroup, cmsCreateParams.RegisteredServerConnectionDetails.ServerName);
                            registeredServer.Create();
                            if (cmsCreateParams.RegisteredServerConnectionDetails != null)
                            {
                                registeredServer.ServerName = cmsCreateParams.RegisteredServerConnectionDetails.ServerName;
                                registeredServer.Description = cmsCreateParams.RegisteredServerDescription;
                                registeredServer.ConnectionString = ConnectionService.BuildConnectionString(cmsCreateParams.RegisteredServerConnectionDetails);
                            }
                            registeredServer.Description = cmsCreateParams.RegisteredServerDescription;
                            await requestContext.SendResult(true);
                        }
                        else
                        {
                            await requestContext.SendResult(false);
                        }
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleListRegisteredServersRequest(ListRegisteredServersParams listServerParams, RequestContext<ListRegisteredServersResult> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleListRegisteredServersRequest");
            try
            {
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        //Validate and create connection
                        ServerConnection serverConn = ValidateAndCreateConnection(listServerParams.ParentOwnerUri);

                        if (serverConn != null)
                        {
                            // Get registered Servers
                            RegisteredServersStore store = new RegisteredServersStore(serverConn);
                            ServerGroup parentGroup = NavigateToServerGroup(store, listServerParams.RelativePath);

                            ListRegisteredServersResult result = GetChildrenfromParentGroup(parentGroup);
                            await requestContext.SendResult(result);
                        }
                        else
                        {
                            await requestContext.SendResult(null);
                        }
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                });
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
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        // Validate and Connect
                        ServerConnection serverConn = ValidateAndCreateConnection(removeServerParams.ParentOwnerUri);
                        if (serverConn != null)
                        {
                            // Get list of registered Servers
                            RegisteredServersStore store = new RegisteredServersStore(serverConn);
                            ServerGroup parentGroup = NavigateToServerGroup(store, removeServerParams.RelativePath, false);
                            if (parentGroup != null)
                            {
                                RegisteredServer regServ = parentGroup.RegisteredServers.OfType<RegisteredServer>().FirstOrDefault(r => r.Name == removeServerParams.RegisteredServerName); // since duplicates are not allowed
                                regServ?.Drop();
                                await requestContext.SendResult(true);
                            }
                        }
                        else
                        {
                            await requestContext.SendResult(false);
                        }
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleAddServerGroupRequest(AddServerGroupParams addServerGroupParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleAddServerGroupRequest");
            try
            {
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        ServerConnection serverConn = ValidateAndCreateConnection(addServerGroupParams.ParentOwnerUri);
                        RegisteredServersStore store = new RegisteredServersStore(serverConn);
                        ServerGroup parentGroup = NavigateToServerGroup(store, addServerGroupParams.RelativePath);

                        // Add the new group (intentionally not cheching existence to reuse the exception message)
                        ServerGroup serverGroup = new ServerGroup(parentGroup, addServerGroupParams.GroupName)
                        {
                            Description = addServerGroupParams.GroupDescription
                        };
                        serverGroup.Create();
                        await requestContext.SendResult(true);
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        public async Task HandleRemoveServerGroupRequest(RemoveServerGroupParams removeServerGroupParams, RequestContext<bool> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleRemoveServerGroupRequest");
            try
            {
                CmsTask = Task.Run(async () =>
                {
                    try
                    {
                        ServerConnection serverConn = ValidateAndCreateConnection(removeServerGroupParams.ParentOwnerUri);
                        if (serverConn != null)
                        {
                            RegisteredServersStore store = new RegisteredServersStore(serverConn);

                            ServerGroup parentGroup = NavigateToServerGroup(store, removeServerGroupParams.RelativePath, false);
                            ServerGroup serverGrouptoRemove = parentGroup.ServerGroups.OfType<ServerGroup>().FirstOrDefault(r => r.Name == removeServerGroupParams.GroupName); // since duplicates are not allowed
                            serverGrouptoRemove?.Drop();
                            await requestContext.SendResult(true);
                        }
                        else
                        {
                            await requestContext.SendResult(false);
                        }
                    }
                    catch (Exception e)
                    {
                        await requestContext.SendError(e);
                    }
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        #endregion

        #region Private methods

        private ServerGroup NavigateToServerGroup(RegisteredServersStore store, string relativePath, bool alreadyParent = true)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return store.DatabaseEngineServerGroup;
            }

            // Get key chain from URN
            Urn urn = new Urn(relativePath);
            SfcKeyChain keyChain = alreadyParent ? new SfcKeyChain(urn, store as ISfcDomain) : new SfcKeyChain(urn, store as ISfcDomain).Parent;

            ServerGroup parentGroup = GetNodeFromKeyChain(keyChain, store.DatabaseEngineServerGroup);
            return parentGroup;
        }

        private ServerGroup GetNodeFromKeyChain(SfcKeyChain keyChain, ServerGroup rootServerGroup)
        {
            if (keyChain == rootServerGroup.KeyChain)
            {
                return rootServerGroup;
            }
            if (keyChain != rootServerGroup.KeyChain)
            {
                var parent = GetNodeFromKeyChain(keyChain.Parent, rootServerGroup);
                if (parent != null && parent is ServerGroup)
                {
                    var server = (parent as ServerGroup).ServerGroups.FirstOrDefault(x => x.KeyChain == keyChain);
                    return server;
                }
            }
            return null;
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

        private ListRegisteredServersResult GetChildrenfromParentGroup(ServerGroup parentGroup)
        {
            var servers = parentGroup.RegisteredServers;
            var groups = parentGroup.ServerGroups;

            // Convert to appropriate variables and return
            var serverResults = new List<RegisteredServerResult>();
            foreach (var s in servers)
            {
                serverResults.Add(new RegisteredServerResult
                {
                    Name = s.Name,
                    ServerName = s.ServerName,
                    Description = s.Description,
                    ConnectionDetails = ConnectionServiceInstance.ParseConnectionString(s.ConnectionString),
                    RelativePath = s.KeyChain.Urn.SafeToString()
                });
            }

            var groupsResults = new List<RegisteredServerGroup>();
            foreach (var s in groups)
            {
                groupsResults.Add(new RegisteredServerGroup
                {
                    Name = s.Name,
                    Description = s.Description,
                    RelativePath = s.KeyChain.Urn.SafeToString()
                });
            }
            ListRegisteredServersResult result = new ListRegisteredServersResult() { RegisteredServersList = serverResults, RegisteredServerGroups = groupsResults };
            return result;
        }

        #endregion

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
        /// Internal variable for testability
        /// </summary>
        internal Task CmsTask { get; set; }
    }
}

