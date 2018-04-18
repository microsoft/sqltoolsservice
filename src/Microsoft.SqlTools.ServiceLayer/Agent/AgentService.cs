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
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Agent Service functionality
    /// </summary>
    public sealed class AgentService
    {        
        private ConnectionService connectionService = null;
        private static readonly Lazy<AgentService> instance = new Lazy<AgentService>(() => new AgentService());

        private IAgentJobsService agentJobsService = null;

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
        /// Internal for testing purposes only
        /// </summary>
        internal IAgentJobsService AgentJobsServiceInstance
        {
            get
            {
                if (this.agentJobsService == null)
                {
                    this.agentJobsService = new AgentJobsService();
                }
                return this.agentJobsService;
            }
            set
            {
               this. agentJobsService = value;
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

            // Jobs request handlers
            this.ServiceHost.SetRequestHandler(AgentJobsRequest.Type, HandleAgentJobsRequest);
            this.ServiceHost.SetRequestHandler(AgentJobHistoryRequest.Type, HandleJobHistoryRequest);
            this.ServiceHost.SetRequestHandler(AgentJobActionRequest.Type, HandleJobActionRequest);

            // Alerts request handlers
            this.ServiceHost.SetRequestHandler(AgentAlertsRequest.Type, HandleAgentAlertsRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentAlertRequest.Type, HandleCreateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentAlertRequest.Type, HandleUpdateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentAlertRequest.Type, HandleDeleteAgentAlertRequest);

        }

        #region "Jobs Handlers"
    
        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleAgentJobsRequest(AgentJobsParams parameters, RequestContext<AgentJobsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentJobsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    List<AgentJobInfo> jobs;
                    var returnValue = this.AgentJobsServiceInstance.GetJobs(connInfo, out jobs);
                    if (returnValue.Succeeded)
                    {
                        result.Succeeded = true;
                        result.Jobs = jobs.ToArray();  
                    }
                    else
                    {
                        result.ErrorMessage = returnValue.ErrorMessage;
                    }
                }

                await requestContext.SendResult(result);
            });            
        }

        /// <summary>
        /// Handle request to get Agent Job history
        /// </summary>
        internal async Task HandleJobHistoryRequest(AgentJobHistoryParams parameters, RequestContext<AgentJobHistoryResult> requestContext) 
        {
            await Task.Run(async () =>
            {
                var result = new AgentJobHistoryResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    List<AgentJobHistoryInfo> jobHistories;
                    var returnValue = this.AgentJobsServiceInstance.GetJobHistory(connInfo, parameters.JobId, out jobHistories);
                    if (returnValue.Succeeded)
                    {
                        result.Succeeded = true;
                        result.Jobs = jobHistories.ToArray();
                    }
                    else
                    {
                        result.ErrorMessage = returnValue.ErrorMessage;
                    }
                }

                await requestContext.SendResult(result);
            });
        }

        /// <summary>
        /// Handle request to Run a Job
        /// </summary>
        internal async Task HandleJobActionRequest(AgentJobActionParams parameters, RequestContext<AgentJobActionResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentJobActionResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    var returnValue = this.AgentJobsServiceInstance.InvokeJobAction(connInfo, parameters.Action, parameters.JobName);
                    if (returnValue.Succeeded)
                    {
                        result.Succeeded = true;
                    }
                    else
                    {
                        result.ErrorMessage = returnValue.ErrorMessage;
                    }               
                }

                await requestContext.SendResult(result);              
            });
        }

        #endregion // "Jobs Handlers"

        #region "Alter Handlers"

        /// <summary>
        /// Handle request to get the alerts list
        /// </summary>        
        internal async Task HandleAgentAlertsRequest(AgentAlertsParams parameters, RequestContext<AgentAlertsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentAlertsResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo != null)
                {
                    /// look up alerts       
                }

                await requestContext.SendResult(result);
            });
        }

        /// <summary>
        /// Handle request to create an alert
        /// </summary>        
        internal async Task HandleCreateAgentAlertRequest(CreateAgentAlertParams parameters, RequestContext<CreateAgentAlertResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new CreateAgentAlertResult();
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);

                if (connInfo != null)
                {
                    DatabaseTaskHelper helper = AdminService.CreateDatabaseTaskHelper(connInfo, databaseExists: true);
                    using (AgentAlert agentAlert = new AgentAlert(helper.DataContainer))
                    {
                        ExecutionMode executionResult;
                        agentAlert.Execute(RunType.RunNow, out executionResult);
                    }       
                }

                await requestContext.SendResult(result);
            });
        }

        /// <summary>
        /// Handle request to update an alert
        /// </summary>        
        internal async Task HandleUpdateAgentAlertRequest(UpdateAgentAlertParams parameters, RequestContext<UpdateAgentAlertResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        /// <summary>
        /// Handle request to delete an alert
        /// </summary>        
        internal async Task HandleDeleteAgentAlertRequest(DeleteAgentAlertParams parameters, RequestContext<DeleteAgentAlertResult> requestContext)
        {
            await requestContext.SendResult(null);
        }

        #endregion // "Alter Handlers"
    }
}
