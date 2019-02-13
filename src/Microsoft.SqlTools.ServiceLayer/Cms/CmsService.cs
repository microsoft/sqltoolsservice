//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Cms.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Cms
{
    /// <summary>
    /// Main class for DacFx service
    /// </summary>
    public class CmsService
    {
        private static object lockObject;
        private static ConnectionService connectionService = null;
        private static RegisteredServersStore registerdServerStore = null;
        private SqlTaskManager sqlTaskManagerInstance = null;
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
            serviceHost.SetRequestHandler(GetRegisteredServerRequest.Type, this.HandleListServerRequest);
        }
        
        /// <summary>
        /// Handles get servers request
        /// </summary>
        /// <returns></returns>
        public async Task HandleListServerRequest(ConnectParams connectionParams, RequestContext<ListCmsServersResult> requestContext)
        {
            try
            {
                ConnectionInfo connectionInfo = null;
                if (!ConnectionServiceInstance.OwnerToConnectionMap.TryGetValue(connectionParams.OwnerUri, out connectionInfo))
                {
                    connectionInfo = new ConnectionInfo(ConnectionServiceInstance.ConnectionFactory, connectionParams.OwnerUri, connectionParams.Connection);
                }

                string connectionString = ConnectionService.BuildConnectionString(connectionInfo.ConnectionDetails);
                ServerConnection conn = new ServerConnection(new SqlConnection(connectionString));

                RegisteredServersStore store = new RegisteredServersStore(conn);
                ServerGroup parentGroup = store.DatabaseEngineServerGroup;

                RegisteredServerCollection servers = parentGroup.RegisteredServers;

                var servernames = new List<string>();
                foreach(var s in servers)
                {
                    servernames.Add(s.Name);
                }

                ListCmsServersResult result = new ListCmsServersResult() { CmsServersList = servernames };
                await requestContext.SendResult(result);
            }

            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
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

