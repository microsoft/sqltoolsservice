//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.HighVariation;
using Microsoft.SqlServer.Management.QueryStoreModel.OverallResourceConsumption;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;
using Microsoft.SqlServer.Management.QueryStoreModel.RegressedQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.Performance;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore
{
    /// <summary>
    /// Main class for Query Store service
    /// </summary>
    public class QueryStoreService : BaseService
    {
        private static readonly Lazy<QueryStoreService> instance = new Lazy<QueryStoreService>(() => new QueryStoreService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static QueryStoreService Instance => instance.Value;

        /// <summary>
        /// Instance of the connection service, used to get the connection info for a given owner URI
        /// </summary>
        private ConnectionService ConnectionService { get; }

        public QueryStoreService()
        {
            ConnectionService = ConnectionService.Instance;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Top Resource Consumers report
            serviceHost.SetRequestHandler(GetTopResourceConsumersSummaryRequest.Type, HandleGetTopResourceConsumersSummaryReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetTopResourceConsumersDetailedSummaryRequest.Type, HandleGetTopResourceConsumersDetailedSummaryReportRequest, isParallelProcessingSupported: true);

            // Forced Plan Queries report
            serviceHost.SetRequestHandler(GetForcedPlanQueriesReportRequest.Type, HandleGetForcedPlanQueriesReportRequest, isParallelProcessingSupported: true);

            // Tracked Queries report
            serviceHost.SetRequestHandler(GetTrackedQueriesReportRequest.Type, HandleGetTrackedQueriesReportRequest, isParallelProcessingSupported: true);

            // High Variation Queries report
            serviceHost.SetRequestHandler(GetHighVariationQueriesSummaryRequest.Type, HandleGetHighVariationQueriesSummaryReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetHighVariationQueriesDetailedSummaryRequest.Type, HandleGetHighVariationQueriesDetailedSummaryReportRequest, isParallelProcessingSupported: true);

            // Overall Resource Consumption report
            serviceHost.SetRequestHandler(GetOverallResourceConsumptionReportRequest.Type, HandleGetOverallResourceConsumptionReportRequest, isParallelProcessingSupported: true);

            // Regressed Queries report
            serviceHost.SetRequestHandler(GetRegressedQueriesSummaryRequest.Type, HandleGetRegressedQueriesSummaryReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetRegressedQueriesDetailedSummaryRequest.Type, HandleGetRegressedQueriesDetailedSummaryReportRequest, isParallelProcessingSupported: true);

            // Plan Summary report
            serviceHost.SetRequestHandler(GetPlanSummaryChartViewRequest.Type, HandleGetPlanSummaryChartViewRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetPlanSummaryGridViewRequest.Type, HandleGetPlanSummaryGridViewRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetForcedPlanRequest.Type, HandleGetForcedPlanRequest, isParallelProcessingSupported: true);
        }

        #region Handlers

        /* 
         * General process is to:
         * 1. Convert the ADS config to the QueryStoreModel config format
         * 2. Call the unordered query generator to get the list of columns
         * 3. Select the intended ColumnInfo for sorting
         * 4. Call the ordered query generator to get the actual query
         * 5. Prepend any necessary TSQL parameters to the generated query
         * 6. Return the query text to ADS for execution
         */

        #region Top Resource Consumers report

        internal async Task HandleGetTopResourceConsumersSummaryReportRequest(GetTopResourceConsumersReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                TopResourceConsumersConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetTopResourceConsumersSummaryReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query,
                };
            }, requestContext);
        }

        internal async Task HandleGetTopResourceConsumersDetailedSummaryReportRequest(GetTopResourceConsumersReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                TopResourceConsumersConfiguration config = requestParams.Convert();
                string query;

                using (SqlConnection connection = GetSqlConnection(requestParams))
                {
                    query = QueryStoreQueryGenerator.GetTopResourceConsumersDetailedSummaryReportQuery(config, connection, requestParams.OrderByColumnId, requestParams.Descending);
                }

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region Forced Plans report

        internal async Task HandleGetForcedPlanQueriesReportRequest(GetForcedPlanQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                ForcedPlanQueriesConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetForcedPlanQueriesReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region Tracked Queries report

        internal async Task HandleGetTrackedQueriesReportRequest(GetTrackedQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                string query = QueryStoreQueryGenerator.GetTrackedQueriesReportQuery(requestParams.QuerySearchText);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region High Variation Queries report

        internal async Task HandleGetHighVariationQueriesSummaryReportRequest(GetHighVariationQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                HighVariationConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetHighVariationQueriesSummaryReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        internal async Task HandleGetHighVariationQueriesDetailedSummaryReportRequest(GetHighVariationQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                HighVariationConfiguration config = requestParams.Convert();
                string query;

                using (SqlConnection connection = GetSqlConnection(requestParams))
                {
                    query = QueryStoreQueryGenerator.GetHighVariationQueriesDetailedSummaryReportQuery(config, connection, requestParams.OrderByColumnId, requestParams.Descending);
                }

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region Overall Resource Consumption report

        internal async Task HandleGetOverallResourceConsumptionReportRequest(GetOverallResourceConsumptionReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                OverallResourceConsumptionConfiguration config = requestParams.Convert();
                string query;

                using (SqlConnection connection = GetSqlConnection(requestParams))
                {
                    query = QueryStoreQueryGenerator.GetOverallResourceConsumptionReportQuery(config, connection);
                }

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region Regressed Queries report

        internal async Task HandleGetRegressedQueriesSummaryReportRequest(GetRegressedQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                RegressedQueriesConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetRegressedQueriesSummaryReportQuery(config);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        internal async Task HandleGetRegressedQueriesDetailedSummaryReportRequest(GetRegressedQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                RegressedQueriesConfiguration config = requestParams.Convert();
                string query;

                using (SqlConnection connection = GetSqlConnection(requestParams))
                {
                    query = QueryStoreQueryGenerator.GetRegressedQueriesDetailedSummaryReportQuery(config, connection);
                }

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #region Plan Summary report

        internal async Task HandleGetPlanSummaryChartViewRequest(GetPlanSummaryParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                PlanSummaryConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetPlanSummaryChartViewQuery(config);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        internal async Task HandleGetPlanSummaryGridViewRequest(GetPlanSummaryGridViewParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                PlanSummaryConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetPlanSummaryGridViewQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        internal async Task HandleGetForcedPlanRequest(GetForcedPlanParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                string query = QueryStoreQueryGenerator.GetForcedPlanQuery(requestParams.QueryId, requestParams.PlanId);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            }, requestContext);
        }

        #endregion

        #endregion

        #region Helpers

        internal virtual SqlConnection GetSqlConnection(QueryStoreReportParams requestParams)
        {
            ConnectionService.TryFindConnection(requestParams.ConnectionOwnerUri, out ConnectionInfo connectionInfo);

            if (connectionInfo != null)
            {
                return ConnectionService.OpenSqlConnection(connectionInfo, "QueryStoreService available metrics");
            }
            else
            {
                throw new InvalidOperationException($"Unable to find connection for '{requestParams.ConnectionOwnerUri}'");
            }
        }

        #endregion
    }
}
