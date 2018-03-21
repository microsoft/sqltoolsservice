//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;


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
            this.ServiceHost.SetRequestHandler(AgentJobsRequest.Type, HandleAgentJobsRequest);
            this.ServiceHost.SetRequestHandler(AgentJobHistoryRequest.Type, HandleJobHistoryRequest);
        }

        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleAgentJobsRequest(AgentJobsParams parameters, RequestContext<AgentJobsResult> requestContext)
        {
            try
            {
                var result = new AgentJobsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);                    
                    var fetcher = new JobFetcher(serverConnection);
                    var filter = new JobActivityFilter();
                    this.jobs = fetcher.FetchJobs(filter);

                    var agentJobs = new List<AgentJobInfo>();
                    if (this.jobs != null)
                    {
                        
                        foreach (var job in this.jobs.Values)
                        {
                            agentJobs.Add(JobUtilities.ConvertToAgentJobInfo(job));
                        }
                    }
                    result.Succeeded = true;
                    result.Jobs = agentJobs.ToArray();
                }
                
                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handle request to get Agent Job history
        /// </summary>
        internal async Task HandleJobHistoryRequest(AgentJobHistoryParams parameters, RequestContext<AgentJobHistoryResult> requestContext) 
        {
            try 
            {
                var result = new AgentJobHistoryResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);     
                    var server = new Server(serverConnection);       
                    var filter = new JobHistoryFilter(); 
                    filter.JobID = new Guid(parameters.JobId);
                    var dt = server.JobServer.EnumJobHistory(filter);

                    var sqlConnInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);

                    int count = dt.Rows.Count;
                    var agentJobs = new List<AgentJobHistoryInfo>();
                    for (int i = 0; i < count; ++i)
                    {
                        var job = dt.Rows[i];
                        agentJobs.Add(JobUtilities.ConvertToAgentJobHistoryInfo(job, sqlConnInfo));
                    }
                    result.Succeeded = true;
                    result.Jobs = agentJobs.ToArray();
                    await requestContext.SendResult(result);
                }
            }
            catch (Exception e) 
            {
                await requestContext.SendError(e);
            }

        }
    }
}
