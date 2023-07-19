//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore
{
    /// <summary>
    /// Main class for SqlProjects service
    /// </summary>
    public sealed class QueryStoreService : BaseService
    {
        private static readonly Lazy<QueryStoreService> instance = new Lazy<QueryStoreService>(() => new QueryStoreService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static QueryStoreService Instance => instance.Value;

        private Lazy<ConcurrentDictionary<string, SqlProject>> projects = new Lazy<ConcurrentDictionary<string, SqlProject>>(() => new ConcurrentDictionary<string, SqlProject>(StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// <see cref="ConcurrentDictionary{String, TSqlModel}"/> that maps Project URI to Project
        /// </summary>
        public ConcurrentDictionary<string, SqlProject> Projects => projects.Value;

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GetForcedPlanQueriesReportRequest.Type, HandleGetForcedPlanQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetHighVariationQueriesReportRequest.Type, HandleGetHighVariationQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetOverallResourceConsumptionReportRequest.Type, HandleGetOverallResourceConsumptionReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetRegressedQueriesReportRequest.Type, HandleGetRegressedQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetTopResourceConsumersReportRequest.Type, HandleGetTopResourceConsumersReportRequest, isParallelProcessingSupported: true);
        }

        #region Handlers

        internal async Task HandleGetForcedPlanQueriesReportRequest(GetForcedPlanQueriesReportParams requestParams, RequestContext<GetForcedPlanQueriesReportResult> requestContext)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        internal async Task HandleGetHighVariationQueriesReportRequest(GetHighVariationQueriesReportParams requestParams, RequestContext<GetHighVariationQueriesReportResult> requestContext)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        internal async Task HandleGetOverallResourceConsumptionReportRequest(GetOverallResourceConsumptionReportParams requestParams, RequestContext<GetOverallResourceConsumptionReportResult> requestContext)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        internal async Task HandleGetRegressedQueriesReportRequest(GetRegressedQueriesReportParams requestParams, RequestContext<GetRegressedQueriesReportResult> requestContext)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        internal async Task HandleGetTopResourceConsumersReportRequest(GetTopResourceConsumersReportParams requestParams, RequestContext<GetTopResourceConsumersReportResult> requestContext)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }

        #endregion
    }
}
