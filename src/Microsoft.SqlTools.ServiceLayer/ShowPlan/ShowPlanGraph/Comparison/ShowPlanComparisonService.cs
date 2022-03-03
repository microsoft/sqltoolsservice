//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph.Comparison;
using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Show Plan Comparison functionality.
    /// </summary>
    public sealed class ShowPlanComparisonService : IDisposable
    {
        private static readonly Lazy<ShowPlanComparisonService> instance = new(() => new ShowPlanComparisonService());

        private bool disposed;

        /// <summary>
        /// Constructs a new ShowPlanComparison instance with default parameters.
        /// </summary>
        public ShowPlanComparisonService() { }

        /// <summary>
        /// Gets singleton instance object.
        /// </summary>
        public static ShowPlanComparisonService Instance => instance.Value;

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
        /// Initializes the ShowPlanComparison service.
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            ServiceHost = serviceHost;
            ServiceHost.SetRequestHandler(GraphComparisonRequest.Type, HandleGraphComparisonRequest);
        }

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

                var skeletonManager = new SkeletonManager();
                var firstSkeletonNode = skeletonManager.CreateSkeleton(firstRootNode);
                var secondSkeletonNode = skeletonManager.CreateSkeleton(secondRootNode);

                var isEquivalent = skeletonManager.AreSkeletonsEquivalent(firstSkeletonNode, secondSkeletonNode, parameter.IgnoreDatabaseName);

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
        /// Disposes the ShowPlanComparison service.
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
