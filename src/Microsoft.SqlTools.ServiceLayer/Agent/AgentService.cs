//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
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


        private const string cUrnEnumerateAgentJobs = "Server/JobServer/Job";
        private const string cUrnJobName = "JobName";
        private const string cUrnJobId = "JobID";
        private const string cUrnJobCategoryId = "RunStatus";

        private void TestApi(Server server)
        {
            var job = this.jobs.Last().Value;            
            //job.

            var filter = new JobHistoryFilter();
            filter.JobID = job.JobID;
            var dt = server.JobServer.EnumJobHistory(filter);

            StringBuilder sb = new StringBuilder();

            int count = dt.Rows.Count;
            for (int i = 0; i < count; ++i)
            {
                string jobName = Convert.ToString(dt.Rows[i][cUrnJobName], System.Globalization.CultureInfo.InvariantCulture);
                int jobCategoryId = Convert.ToInt32(dt.Rows[i][cUrnJobCategoryId], System.Globalization.CultureInfo.InvariantCulture);
                Guid jobId = (Guid) (dt.Rows[i][cUrnJobId]);

                sb.AppendFormat("{0}, {1}, {2}\n", jobId, jobName, jobCategoryId);

              //  logSources[i] = new LogSourceJobHistory(jobName, sqlCi, customCommandHandler, jobCategoryId, jobId, this.serviceProvider);
            }


            string outp = sb.ToString();


        }


        /*
        
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
                    this.jobs = fetcher.FetchJobs(filter);

                    TestApi(server);
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
