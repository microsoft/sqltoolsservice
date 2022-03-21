//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecutionGraph.Comparison;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan
{
    /// <summary>
    /// Main class for Execution Plan Service functionality
    /// </summary>
    public sealed class ExecutionPlanService : IDisposable
    {
        private static readonly Lazy<ExecutionPlanService> instance = new Lazy<ExecutionPlanService>(() => new ExecutionPlanService());

        private bool disposed;

        /// <summary>
        /// Construct a new ExecutionPlanService instance with default parameters
        /// </summary>
        private ExecutionPlanService()
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
        internal IProtocolEndpoint ServiceHost { get; set; }

        /// <summary>
        /// Initializes the ShowPlan Service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            ServiceHost = serviceHost;
            ServiceHost.SetRequestHandler(GetExecutionPlanRequest.Type, HandleGetExecutionPlan);
            ServiceHost.SetRequestHandler(GraphComparisonRequest.Type, HandleGraphComparisonRequest);
        }

        private async Task HandleGetExecutionPlan(GetExecutionPlanParams requestParams, RequestContext<GetExecutionPlanResult> requestContext)
        {
            try
            {
                var plans = ExecutionPlanGraphUtils.CreateShowPlanGraph(requestParams.GraphInfo.GraphFileContent, "");
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
        /// Handles requests for color matching similar nodes.
        /// </summary>
        internal async Task HandleGraphComparisonRequest(
            GraphComparisonParams requestParams,
            RequestContext<GraphComparisonResult> requestContext)
        {
            try
            {
                var firstGraphSet = ShowPlanGraph.ParseShowPlanXML(requestParams.FirstExecutionPlanGraphInfo.GraphFileContent, ShowPlanType.Unknown);
                var firstRootNode = firstGraphSet?[0]?.Root;

                var secondGraphSet = ShowPlanGraph.ParseShowPlanXML(requestParams.SecondExecutionPlanGraphInfo.GraphFileContent, ShowPlanType.Unknown);
                var secondRootNode = secondGraphSet?[0]?.Root;

                var manager = new SkeletonManager();
                var firstSkeletonNode = manager.CreateSkeleton(firstRootNode);
                var secondSkeletonNode = manager.CreateSkeleton(secondRootNode);
                manager.ColorMatchingSections(firstSkeletonNode, secondSkeletonNode, requestParams.IgnoreDatabaseName);

                var firstGraphComparisonResultDTO = firstSkeletonNode.ConvertToDTO();
                var secondGraphComparisonResultDTO = secondSkeletonNode.ConvertToDTO();
                ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(firstGraphComparisonResultDTO, secondGraphComparisonResultDTO);
                ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(secondGraphComparisonResultDTO, firstGraphComparisonResultDTO);

                var result = new GraphComparisonResult()
                {
                    FirstComparisonResult = firstGraphComparisonResultDTO,
                    SecondComparisonResult = secondGraphComparisonResultDTO
                };

                await requestContext.SendResult(result);
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
