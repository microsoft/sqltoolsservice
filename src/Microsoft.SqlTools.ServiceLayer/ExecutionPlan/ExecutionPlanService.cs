//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph.Comparison;

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
            ServiceHost.SetRequestHandler(ColorMatchingSectionsRequest.Type, HandleColorMatchingRequest);
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
        internal async Task HandleColorMatchingRequest(
            ColorMatchingSectionsParams parameter,
            RequestContext<ColorMatchingSectionsResult> requestContext)
        {
            try
            {
                var firstGraphSet = ShowPlanGraph.ParseShowPlanXML(parameter.FirstQueryPlanXmlText, ShowPlanType.Unknown);
                var firstRootNode = firstGraphSet?[0]?.Root;

                var secondGraphSet = ShowPlanGraph.ParseShowPlanXML(parameter.SecondQueryPlanXmlText, ShowPlanType.Unknown);
                var secondRootNode = secondGraphSet?[0]?.Root;

                var manager = new SkeletonManager();
                var firstSkeletonNode = manager.CreateSkeleton(firstRootNode);
                var secondSkeletonNode = manager.CreateSkeleton(secondRootNode);
                manager.ColorMatchingSections(firstSkeletonNode, secondSkeletonNode, parameter.IgnoreDatabaseName);

                var firstSkeletonNodeDTO = firstSkeletonNode.ConvertToDTO();
                var secondSkeletonNodeDTO = secondSkeletonNode.ConvertToDTO();
                ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(firstSkeletonNodeDTO, secondSkeletonNodeDTO);
                ExecutionPlanGraphUtils.CopyMatchingNodesIntoSkeletonDTO(secondSkeletonNodeDTO, firstSkeletonNodeDTO);

                var result = new ColorMatchingSectionsResult()
                {
                    FirstSkeletonNode = firstSkeletonNodeDTO,
                    SecondSkeletonNode = secondSkeletonNodeDTO
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
