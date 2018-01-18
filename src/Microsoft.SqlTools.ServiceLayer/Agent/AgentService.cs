//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public sealed class AgentService
    {
        private JobActivityFilter filter = null;
        private Dictionary<Guid, JobProperties> jobs = null;

        private JobFetcher fetcher = null;

        private ConnectionService connectionService = null;

        private static readonly Lazy<AgentService> instance = new Lazy<AgentService>(() => new AgentService());

        /// <summary>
        /// Construct a new AgentService instance with default parameters
        /// </summary>
        public AgentService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AgentService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
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
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(GetAgentJobActivityRequest.Type, HandleGetAgentJobActivityRequest);
        }

        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleGetAgentJobActivityRequest(GetAgentJobActivityParams parameters, RequestContext<GetAgentJobActivityResult> requestContext)
        {
            try
            {
                var result = new GetAgentJobActivityResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);
                    var server = new Server(serverConnection);
                    var fetcher = new JobFetcher(serverConnection);
                    var filter = new JobActivityFilter();
                    var jobs = fetcher.FetchJobs(filter);
                }

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }
    }
}
