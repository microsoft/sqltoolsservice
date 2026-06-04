//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.SqlCore.Performance.ForcedPlanQueries;
using Microsoft.SqlTools.SqlCore.Performance.HighVariation;
using Microsoft.SqlTools.SqlCore.Performance.OverallResourceConsumption;
using Microsoft.SqlTools.SqlCore.Performance.PlanSummary;
using Microsoft.SqlTools.SqlCore.Performance.RegressedQueries;
using Microsoft.SqlTools.SqlCore.Performance.TopResourceConsumers;
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
            serviceHost.RegisterRequestHandler(GetTopResourceConsumersSummaryRequest.Type, HandleGetTopResourceConsumersSummaryReportRequest);
            serviceHost.RegisterRequestHandler(GetTopResourceConsumersDetailedSummaryRequest.Type, HandleGetTopResourceConsumersDetailedSummaryReportRequest);

            // Forced Plan Queries report
            serviceHost.RegisterRequestHandler(GetForcedPlanQueriesReportRequest.Type, HandleGetForcedPlanQueriesReportRequest);

            // Tracked Queries report
            serviceHost.RegisterRequestHandler(GetTrackedQueriesReportRequest.Type, HandleGetTrackedQueriesReportRequest);

            // High Variation Queries report
            serviceHost.RegisterRequestHandler(GetHighVariationQueriesSummaryRequest.Type, HandleGetHighVariationQueriesSummaryReportRequest);
            serviceHost.RegisterRequestHandler(GetHighVariationQueriesDetailedSummaryRequest.Type, HandleGetHighVariationQueriesDetailedSummaryReportRequest);

            // Overall Resource Consumption report
            serviceHost.RegisterRequestHandler(GetOverallResourceConsumptionReportRequest.Type, HandleGetOverallResourceConsumptionReportRequest);

            // Regressed Queries report
            serviceHost.RegisterRequestHandler(GetRegressedQueriesSummaryRequest.Type, HandleGetRegressedQueriesSummaryReportRequest);
            serviceHost.RegisterRequestHandler(GetRegressedQueriesDetailedSummaryRequest.Type, HandleGetRegressedQueriesDetailedSummaryReportRequest);

            // Plan Summary report
            serviceHost.RegisterRequestHandler(GetPlanSummaryChartViewRequest.Type, HandleGetPlanSummaryChartViewRequest);
            serviceHost.RegisterRequestHandler(GetPlanSummaryGridViewRequest.Type, HandleGetPlanSummaryGridViewRequest);
            serviceHost.RegisterRequestHandler(GetForcedPlanRequest.Type, HandleGetForcedPlanRequest);
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

        internal async Task<QueryStoreQueryResult> HandleGetTopResourceConsumersSummaryReportRequest(GetTopResourceConsumersReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                TopResourceConsumersConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetTopResourceConsumersSummaryReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query,
                };
            });
        }

        internal async Task<QueryStoreQueryResult> HandleGetTopResourceConsumersDetailedSummaryReportRequest(GetTopResourceConsumersReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
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
            });
        }

        #endregion

        #region Forced Plans report

        internal async Task<QueryStoreQueryResult> HandleGetForcedPlanQueriesReportRequest(GetForcedPlanQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                ForcedPlanQueriesConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetForcedPlanQueriesReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        #endregion

        #region Tracked Queries report

        internal async Task<QueryStoreQueryResult> HandleGetTrackedQueriesReportRequest(GetTrackedQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                string query = QueryStoreQueryGenerator.GetTrackedQueriesReportQuery(requestParams.QuerySearchText);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        #endregion

        #region High Variation Queries report

        internal async Task<QueryStoreQueryResult> HandleGetHighVariationQueriesSummaryReportRequest(GetHighVariationQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                HighVariationConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetHighVariationQueriesSummaryReportQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        internal async Task<QueryStoreQueryResult> HandleGetHighVariationQueriesDetailedSummaryReportRequest(GetHighVariationQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
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
            });
        }

        #endregion

        #region Overall Resource Consumption report

        internal async Task<QueryStoreQueryResult> HandleGetOverallResourceConsumptionReportRequest(GetOverallResourceConsumptionReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
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
            });
        }

        #endregion

        #region Regressed Queries report

        internal async Task<QueryStoreQueryResult> HandleGetRegressedQueriesSummaryReportRequest(GetRegressedQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                RegressedQueriesConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetRegressedQueriesSummaryReportQuery(config);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        internal async Task<QueryStoreQueryResult> HandleGetRegressedQueriesDetailedSummaryReportRequest(GetRegressedQueriesReportParams requestParams)
        {
            return await RunWithErrorHandling(() =>
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
            });
        }

        #endregion

        #region Plan Summary report

        internal async Task<QueryStoreQueryResult> HandleGetPlanSummaryChartViewRequest(GetPlanSummaryParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                PlanSummaryConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetPlanSummaryChartViewQuery(config);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        internal async Task<QueryStoreQueryResult> HandleGetPlanSummaryGridViewRequest(GetPlanSummaryGridViewParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                PlanSummaryConfiguration config = requestParams.Convert();
                string query = QueryStoreQueryGenerator.GetPlanSummaryGridViewQuery(config, requestParams.OrderByColumnId, requestParams.Descending);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
        }

        internal async Task<QueryStoreQueryResult> HandleGetForcedPlanRequest(GetForcedPlanParams requestParams)
        {
            return await RunWithErrorHandling(() =>
            {
                string query = QueryStoreQueryGenerator.GetForcedPlanQuery(requestParams.QueryId, requestParams.PlanId);

                return new QueryStoreQueryResult()
                {
                    Success = true,
                    ErrorMessage = null,
                    Query = query
                };
            });
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
