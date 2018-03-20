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
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Profiler Service functionality
    /// </summary>
    public sealed class AgentService
    {
        private Dictionary<Guid, JobProperties> jobs = null;
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
            this.ServiceHost.SetRequestHandler(AgentJobActionRequest.Type, HandleJobActionRequest);
        }

        private void TestApi(ServerConnection serverConnection)
        {
            var server = new Server(serverConnection);
            var job = this.jobs.Last().Value;            

            var filter = new JobHistoryFilter();
            filter.JobID = job.JobID;
            var dt = server.JobServer.EnumJobHistory(filter);

            StringBuilder sb = new StringBuilder();

            var connInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);

            int count = dt.Rows.Count;
            for (int i = 0; i < count; ++i)
            {
                string jobName = Convert.ToString(dt.Rows[i][JobUtilities.UrnJobName], System.Globalization.CultureInfo.InvariantCulture);
                int jobCategoryId = Convert.ToInt32(dt.Rows[i][JobUtilities.UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
                Guid jobId = (Guid) (dt.Rows[i][JobUtilities.UrnJobId]);

                sb.AppendFormat("{0}, {1}, {2}\n", jobId, jobName, jobCategoryId);

                var t = new LogSourceJobHistory(jobName, connInfo, null, jobCategoryId, jobId, null);
                var tlog = t as ILogSource;
                tlog.Initialize();
            }

            string outp = sb.ToString();
        }


        /* alternate approach to get DataTable
            Request req = new Request();
            Enumerator en = new Enumerator();

            req.Urn = cUrnEnumerateAgentJobs;
            req.Fields = new string[] { cUrnJobName, cUrnJobCategoryId, cUrnJobId };
            req.OrderByList = new OrderBy[] { new OrderBy(cUrnJobName, OrderBy.Direction.Asc) };

            DataTable dt = en.Process(sqlCi, req);
            int count = dt.Rows.Count;

            logSources = new ILogSource[count];
            for (int i = 0; i < count; ++i)
            {
                string jobName = Convert.ToString(dt.Rows[i][cUrnJobName], System.Globalization.CultureInfo.InvariantCulture);
                int jobCategoryId = Convert.ToInt32(dt.Rows[i][cUrnJobCategoryId], System.Globalization.CultureInfo.InvariantCulture);
                Guid jobId = (Guid) (dt.Rows[i][cUrnJobId]);

                logSources[i] = new LogSourceJobHistory(jobName, sqlCi, customCommandHandler, jobCategoryId, jobId, this.serviceProvider);
            }
         */

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
                    Tuple<SqlConnectionInfo, DataTable> tuple = CreateSqlConnection(connInfo, parameters.JobId);
                    SqlConnectionInfo sqlConnInfo = tuple.Item1;
                    DataTable dt = tuple.Item2;
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

        /// <summary>
        /// Handle request to Run a Job
        /// </summary>
        internal async Task HandleJobActionRequest(AgentJobActionParams parameters, RequestContext<AgentJobActionResult> requestContext)
        {
            try 
            {
                var result = new AgentJobActionResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                    var serverConnection = new ServerConnection(sqlConnection);     
                    var jobHelper = new JobHelper(serverConnection);
                    jobHelper.JobName = parameters.JobName;
                    switch(parameters.Action)
                    {
                        case "run":
                            jobHelper.Start();
                            break;
                        case "stop":
                            jobHelper.Stop();
                            break;
                        case "delete":
                            jobHelper.Delete();
                            break;
                        case "enable":
                            jobHelper.Enable(true);
                            break;
                        case "disable":
                            jobHelper.Enable(false);
                            break;
                        default:
                            break;
                    }
                    result.Succeeded = true;
                    await requestContext.SendResult(result);
                }
            }
            catch (Exception e) 
            {
                await requestContext.SendError(e);
            }
        }

        private Tuple<SqlConnectionInfo, DataTable> CreateSqlConnection(ConnectionInfo connInfo, String jobId)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            var serverConnection = new ServerConnection(sqlConnection);     
            var server = new Server(serverConnection);       
            var filter = new JobHistoryFilter(); 
            filter.JobID = new Guid(jobId);
            var dt = server.JobServer.EnumJobHistory(filter);
            var sqlConnInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);
            return new Tuple<SqlConnectionInfo, DataTable>(sqlConnInfo, dt);
        }

    }
}
