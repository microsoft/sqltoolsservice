//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph.Comparison;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Migration Service functionality
    /// </summary>
    public sealed class ShowPlanService : IDisposable
    {
        private static readonly Lazy<ShowPlanService> instance = new Lazy<ShowPlanService>(() => new ShowPlanService());

        private bool disposed;

        /// <summary>
        /// Construct a new MigrationService instance with default parameters
        /// </summary>
        public ShowPlanService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ShowPlanService Instance
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
            ServiceHost.SetRequestHandler(CreateSkeletonRequest.Type, HandleCreateSkeletonRequest);
            ServiceHost.SetRequestHandler(GraphComparisonRequest.Type, HandleGraphComparisonRequest);
            ServiceHost.SetRequestHandler(ColorMatchingSectionsRequest.Type, HandleColorMatchingRequest);
            ServiceHost.SetRequestHandler(FindNextNonIgnoreNodeRequest.Type, HandleFindNextNonIgnoreNodeRequest);
        }

        /// <summary>
        /// Handles requests to create skeletons.
        /// </summary>
        internal async Task HandleCreateSkeletonRequest(
            CreateSkeletonParams parameter,
            RequestContext<CreateSkeletonResult> requestContext)
        {
            try
            {
                var graph = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(parameter.QueryPlanXmlText, ShowPlanType.Unknown);
                var root = graph?[0]?.Root;

                var manager = new SkeletonManager();
                var skeletonNode = manager.CreateSkeleton(root);

                var result = new CreateSkeletonResult()
                {
                    SkeletonNode = skeletonNode.ConvertToDTO()
                };

                await requestContext.SendResult(result);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        /// <summary>
        /// Handles requests to compare graphs
        /// </summary>
        internal async Task HandleGraphComparisonRequest(
            GetGraphComparisonParams parameter,
            RequestContext<GetGraphComparisonResult> requestContext)
        {
            try
            {
                var firstGraphSet = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(parameter.FirstQueryPlanXmlText, ShowPlanType.Unknown);
                var firstRootNode = firstGraphSet?[0]?.Root;

                var secondGraphSet = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(parameter.SecondQueryPlanXmlText, ShowPlanType.Unknown);
                var secondRootNode = secondGraphSet?[0]?.Root;

                var manager = new SkeletonManager();
                var firstSkeletonNode = manager.CreateSkeleton(firstRootNode);
                var secondSkeletonNode = manager.CreateSkeleton(secondRootNode);
                var isEquivalent = manager.AreSkeletonsEquivalent(firstSkeletonNode, secondSkeletonNode, parameter.IgnoreDatabaseName);

                var result = new GetGraphComparisonResult()
                {
                    IsEquivalent = isEquivalent
                };

                await requestContext.SendResult(result);
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
                var firstGraphSet = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(parameter.FirstQueryPlanXmlText, ShowPlanType.Unknown);
                var firstRootNode = firstGraphSet?[0]?.Root;

                var secondGraphSet = ShowPlanGraph.ShowPlanGraph.ParseShowPlanXML(parameter.SecondQueryPlanXmlText, ShowPlanType.Unknown);
                var secondRootNode = secondGraphSet?[0]?.Root;

                var manager = new SkeletonManager();
                var firstSkeletonNode = manager.CreateSkeleton(firstRootNode);
                var secondSkeletonNode = manager.CreateSkeleton(secondRootNode);
                manager.ColorMatchingSections(firstSkeletonNode, secondSkeletonNode, parameter.IgnoreDatabaseName);

                var firstSkeletonNodeDTO = firstSkeletonNode.ConvertToDTO();
                var secondSkeletonNodeDTO = secondSkeletonNode.ConvertToDTO();
                ShowPlanGraphUtils.CopyMatchingNodesForSKeletonDTO(firstSkeletonNodeDTO, secondSkeletonNodeDTO);
                ShowPlanGraphUtils.CopyMatchingNodesForSKeletonDTO(secondSkeletonNodeDTO, firstSkeletonNodeDTO);

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
        /// Handles request to locate the next node that should not be
        /// ignored during show plan comparisons.
        /// </summary>
        internal async Task HandleFindNextNonIgnoreNodeRequest(
            FindNextNonIgnoreNodeParams parameter,
            RequestContext<FindNextNonIgnoreNodeResult> requestContext)
        {
            try
            {
                var manager = new SkeletonManager();
                var nextNonIgnoreNode = manager.FindNextNonIgnoreNode(parameter.Node);

                var result = new FindNextNonIgnoreNodeResult()
                {
                    NextNonIgnoreNode = nextNonIgnoreNode.ConvertToDTO()
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
