//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class ExecutionPlanService : IDisposable
    {
        private static readonly Lazy<ExecutionPlanService> instance = new Lazy<ExecutionPlanService>(() => new ExecutionPlanService());

        private bool disposed;

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public ExecutionPlanService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ExecutionPlanService Instance
        {
            get { return instance.Value; }
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
        /// Initializes the ShowPlan Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;
            this.ServiceHost.SetRequestHandler(GetExecutionPlanRequest.Type, HandleGetExecutionPlan);
        }

        private async Task HandleGetExecutionPlan(GetExecutionPlanParams requestParams, RequestContext<GetExecutionPlanResult> requestContext)
        {
            try
            {
                var plans = ShowPlanGraphUtils.CreateShowPlanGraph(requestParams.GraphInfo.GraphFileContent, "");
                await requestContext.SendResult(new GetExecutionPlanResult
                {
                    Graphs = plans
                });
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        /// <summary>
        /// Disposes the ShowPlan Service
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
            }
        }
    }
}
