//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.HighVariation;
using Microsoft.SqlServer.Management.QueryStoreModel.OverallResourceConsumption;
using Microsoft.SqlServer.Management.QueryStoreModel.PlanSummary;
using Microsoft.SqlServer.Management.QueryStoreModel.RegressedQueries;
using Microsoft.SqlServer.Management.QueryStoreModel.TopResourceConsumers;
using Microsoft.SqlServer.Management.QueryStoreModel.TrackedQueries;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryStore.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryStore
{
    /// <summary>
    /// Main class for SqlProjects service
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

        private QueryStoreService()
        {
            ConnectionService = ConnectionService.Instance;
        }

        internal QueryStoreService(ConnectionService connService)
        {
            ConnectionService = connService;
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
                TopResourceConsumersQueryGenerator.TopResourceConsumersSummary(config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = TopResourceConsumersQueryGenerator.TopResourceConsumersSummary(config, orderByColumn, requestParams.Descending, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                    [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
                };

                if (!config.ReturnAllQueries)
                {
                    sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
                }

                query = PrependSqlParameters(query, sqlParams);

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
                TopResourceConsumersQueryGenerator.TopResourceConsumersDetailedSummary(GetAvailableMetrics(requestParams), config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = TopResourceConsumersQueryGenerator.TopResourceConsumersDetailedSummary(GetAvailableMetrics(requestParams), config, orderByColumn, requestParams.Descending, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                    [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
                };

                if (!config.ReturnAllQueries)
                {
                    sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
                }

                query = PrependSqlParameters(query, sqlParams);

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
                ForcedPlanQueriesQueryGenerator.ForcedPlanQueriesSummary(config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = ForcedPlanQueriesQueryGenerator.ForcedPlanQueriesSummary(config, orderByColumn, requestParams.Descending, out IList<ColumnInfo> _);

                if (!config.ReturnAllQueries)
                {
                    query = PrependSqlParameters(query, new() { [QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned.ToString() });
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

        #region Tracked Queries report

        internal async Task HandleGetTrackedQueriesReportRequest(GetTrackedQueriesReportParams requestParams, RequestContext<QueryStoreQueryResult> requestContext)
        {
            await RunWithErrorHandling(() =>
            {
                string query = QueryIDSearchQueryGenerator.GetQuery();

                query = PrependSqlParameters(query, new() { [QueryIDSearchQueryGenerator.QuerySearchTextParameter] = requestParams.QuerySearchText });

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
                HighVariationQueryGenerator.HighVariationSummary(config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = HighVariationQueryGenerator.HighVariationSummary(config, orderByColumn, requestParams.Descending, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                    [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
                };

                if (!config.ReturnAllQueries)
                {
                    sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
                }

                query = PrependSqlParameters(query, sqlParams);

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
                IList<Metric> availableMetrics = GetAvailableMetrics(requestParams);
                HighVariationQueryGenerator.HighVariationDetailedSummary(availableMetrics, config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = HighVariationQueryGenerator.HighVariationDetailedSummary(availableMetrics, config, orderByColumn, requestParams.Descending, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset,
                    [QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset
                };

                if (!config.ReturnAllQueries)
                {
                    sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
                }

                query = PrependSqlParameters(query, sqlParams);

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
                string query = OverallResourceConsumptionQueryGenerator.GenerateQuery(GetAvailableMetrics(requestParams), config, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterIntervalStartTime] = config.SpecifiedTimeInterval.StartDateTimeOffset,
                    [QueryGeneratorUtils.ParameterIntervalEndTime] = config.SpecifiedTimeInterval.EndDateTimeOffset
                };

                query = PrependSqlParameters(query, sqlParams);

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
                string query = RegressedQueriesQueryGenerator.RegressedQuerySummary(config, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [RegressedQueriesQueryGenerator.ParameterRecentStartTime] = config.TimeIntervalRecent.StartDateTimeOffset,
                    [RegressedQueriesQueryGenerator.ParameterRecentEndTime] = config.TimeIntervalRecent.EndDateTimeOffset,
                    [RegressedQueriesQueryGenerator.ParameterHistoryStartTime] = config.TimeIntervalHistory.StartDateTimeOffset,
                    [RegressedQueriesQueryGenerator.ParameterHistoryEndTime] = config.TimeIntervalHistory.EndDateTimeOffset,
                    [RegressedQueriesQueryGenerator.ParameterMinExecutionCount] = config.MinExecutionCount
                };

                if (!config.ReturnAllQueries)
                {
                    sqlParams[QueryGeneratorUtils.ParameterResultsRowCount] = config.TopQueriesReturned;
                }

                query = PrependSqlParameters(query, sqlParams);


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
                string query = RegressedQueriesQueryGenerator.RegressedQueryDetailedSummary(GetAvailableMetrics(requestParams), config, out _);

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

                BucketInterval bucketInterval = BucketInterval.Hour;

                // if interval is specified then select a 'good' interval
                if (config.UseTimeInterval)
                {
                    TimeSpan duration = config.TimeInterval.TimeSpan;
                    bucketInterval = BucketIntervalUtils.CalculateGoodSubInterval(duration);
                }

                string query = PlanSummaryQueryGenerator.PlanSummaryChartView(config, bucketInterval, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterQueryId] = config.QueryId
                };

                if (config.UseTimeInterval)
                {
                    sqlParams[QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset;
                    sqlParams[QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset;
                }

                query = PrependSqlParameters(query, sqlParams);

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

                PlanSummaryQueryGenerator.PlanSummaryGridView(config, out IList<ColumnInfo> columns);
                ColumnInfo orderByColumn = GetOrderByColumn(requestParams, columns);

                string query = PlanSummaryQueryGenerator.PlanSummaryGridView(config, orderByColumn, requestParams.Descending, out _);

                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterQueryId] = config.QueryId
                };

                if (config.UseTimeInterval)
                {
                    sqlParams[QueryGeneratorUtils.ParameterIntervalStartTime] = config.TimeInterval.StartDateTimeOffset;
                    sqlParams[QueryGeneratorUtils.ParameterIntervalEndTime] = config.TimeInterval.EndDateTimeOffset;
                }

                query = PrependSqlParameters(query, sqlParams);

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
                string query = PlanSummaryQueryGenerator.GetForcedPlanQuery();
                Dictionary<string, object> sqlParams = new()
                {
                    [QueryGeneratorUtils.ParameterQueryId] = requestParams.QueryId,
                    [QueryGeneratorUtils.ParameterPlanId] = requestParams.PlanId,
                };

                query = PrependSqlParameters(query, sqlParams);

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

        private ColumnInfo GetOrderByColumn(IOrderableQueryParams requestParams, IList<ColumnInfo> columnInfoList)
        {
            return requestParams.GetOrderByColumnId() != null ? columnInfoList.First(col => col.GetQueryColumnLabel() == requestParams.GetOrderByColumnId()) : columnInfoList[0];
        }

        internal virtual IList<Metric> GetAvailableMetrics(QueryStoreReportParams requestParams)
        {
            ConnectionService.TryFindConnection(requestParams.ConnectionOwnerUri, out ConnectionInfo connectionInfo);

            if (connectionInfo != null)
            {
                using (SqlConnection connection = ConnectionService.OpenSqlConnection(connectionInfo, "QueryStoreService available metrics"))
                {
                    return QdsMetadataMapper.GetAvailableMetrics(connection);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unable to find connection for '{requestParams.ConnectionOwnerUri}'");
            }
        }

        /// <summary>
        /// Prepends declarations and definitions of <paramref name="sqlParams"/> to <paramref name="query"/>
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sqlParams"></param>
        /// <returns></returns>
        private static string PrependSqlParameters(string query, Dictionary<string, object> sqlParams)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string key in sqlParams.Keys)
            {
                sb.AppendLine($"DECLARE {key} {GetTSqlRepresentation(sqlParams[key])};");
            }

            sb.AppendLine();
            sb.AppendLine(query);

            return sb.ToString().Trim();
        }

        private static HashSet<Type> types = new HashSet<Type>();

        /// <summary>
        /// Converts an object (that would otherwise be set as a SqlParameter value) to an entirely TSQL representation.
        /// Only handles the same subset of object types that Query Store query generators use:
        /// int, long, string, and DateTimeOffset
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns>data type and value portions of a parameter declaration, in the form "INT = 999"</returns>
        internal static string GetTSqlRepresentation(object paramValue)
        {
            types.Add(paramValue.GetType());

            switch (paramValue)
            {
                case int i:
                    return $"INT = {i}";
                case long l:
                    return $"BIGINT = {l}";
                case string s:
                    return $"NVARCHAR(max) = N'{s}'"; // TODO: escape
                case DateTimeOffset dto:
                    return $"DATETIMEOFFSET = '{dto.ToString("O", CultureInfo.InvariantCulture)}'"; // "O" = ISO 8601 standard datetime format
                default:
                    Debug.Fail($"Unhandled TSQL parameter type: '{paramValue.GetType()}'");
                    return $"= {paramValue}";
            }
        }

        #endregion
    }
}
