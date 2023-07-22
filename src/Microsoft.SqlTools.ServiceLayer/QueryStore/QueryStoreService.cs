//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlServer.Management.QueryStoreModel.Common;
using Microsoft.SqlServer.Management.QueryStoreModel.ForcedPlanQueries;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
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
            serviceHost.SetRequestHandler(GetForcedPlanQueriesReportRequest.Type, HandleGetForcedPlanQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetHighVariationQueriesReportRequest.Type, HandleGetHighVariationQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetOverallResourceConsumptionReportRequest.Type, HandleGetOverallResourceConsumptionReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetRegressedQueriesReportRequest.Type, HandleGetRegressedQueriesReportRequest, isParallelProcessingSupported: true);
            serviceHost.SetRequestHandler(GetTopResourceConsumersReportRequest.Type, HandleGetTopResourceConsumersReportRequest, isParallelProcessingSupported: true);
        }

        #region Handlers

        internal async Task HandleGetForcedPlanQueriesReportRequest(GetForcedPlanQueriesReportParams requestParams, RequestContext<SimpleExecuteResult> requestContext)
        {
            // Generate query
            ForcedPlanQueriesConfiguration config = new();

            ColumnInfo orderByColumn = GetOrderByColumn(requestParams);

            string query = ForcedPlanQueriesQueryGenerator.ForcedPlanQueriesSummary(config, orderByColumn, descending: true, out IList<ColumnInfo> _ /* discarded because SimpleExecuteResult already extracts this data */);

            await ExecuteQueryHelper(query, requestParams.ConnectionOwnerUri, requestContext);
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

        #region Helpers

        private async Task ExecuteQueryHelper(string query, string connectionOwnerUri, RequestContext<SimpleExecuteResult> requestContext)
        {
            SimpleExecuteParams remappedParams = new()
            {
                OwnerUri = connectionOwnerUri,
                QueryString = query
            };

            await QueryExecutionService.Instance.HandleSimpleExecuteRequest(remappedParams, requestContext);
        }

        // TODO: possibly push down to QueryStoreModel
        private ColumnInfo GetOrderByColumn(QueryStoreReportParams requestParams)
        {
            switch (requestParams.OrderByColumnId)
            {
                case "query_id":
                    return new QueryIdColumnInfo();
                case "object_id":
                    return new ObjectIdColumnInfo();
                case "object_name":
                    return new ObjectNameColumnInfo();
                case "query_sql_text":
                    return new QueryTextColumnInfo();
                case "plan_id":
                    return new PlanIdColumnInfo(); // doubled-up columnId with ForcedPlanFailureCountColumnInfo, may need extra logic here
                case "execution_type":
                    return new ExecutionTypeColumnInfo();
                case "wait_category_desc":
                    return new WaitCategoryDescColumnInfo();
                case "wait_category":
                    return new WaitCategoryIdColumnInfo();
                case "num_plans":
                    return new NumPlansColumnInfo();
                case "force_failure_count":
                    return new ForcedPlanFailureCountColumnInfo();
                case "last_force_failure_reason_desc":
                    return new ForcedPlanFailureDescpColumnInfo();
                case "last_compile_start_time":
                    return new LastCompileStartTimeColumnInfo();
                case "last_execution_time":
                    return new LastForcedPlanExecTimeColumnInfo(); // also LastQueryExecTimeColumnInfo, LastExecTimeColumnInfo
                case "is_forced_plan":
                    return new PlanForcedColumnInfo();
                case "first_execution_time":
                    return new FirstExecTimeColumnInfo();
                case "bucket_start":
                    return new BucketStartTimeColumnInfo();
                case "bucket_end":
                    return new BucketEndTimeColumnInfo();
                default:
                    Debug.Fail($"Unhandled OrderByColumnId: '{requestParams.OrderByColumnId}'");
                    return new QueryIdColumnInfo(); // TODO: is this the correct choice?
            }
        }

        private async Task ExecuteQueryHelperCustom<T>(string query, string connectionOwnerUri, RequestContext<T> requestContext) where T : ResultStatus
        {
            // set up query params
            string queryOwnerUri = Guid.NewGuid().ToString(); // generate guid as the owner uri to make sure every query is unique

            ExecuteStringParams executeStringParams = new ExecuteStringParams
            {
                Query = query,
                OwnerUri = queryOwnerUri
            };

            // set up connection

            if (!ConnectionService.TryFindConnection(connectionOwnerUri, out ConnectionInfo originConnectionInfo))
            {
                await requestContext.SendError(SR.QueryServiceQueryInvalidOwnerUri);
                return;
            }

            ConnectParams connectParams = new ConnectParams
            {
                OwnerUri = queryOwnerUri,
                Connection = originConnectionInfo.ConnectionDetails,
                Type = ConnectionType.Default
            };

            await ConnectionService.Connect(connectParams);
            ConnectionService.TryFindConnection(queryOwnerUri, out ConnectionInfo queryConnectionInfo);

            // set up handlers
            ResultOnlyContext<T> resultContext = new ResultOnlyContext<T>(requestContext);

            Func<string, Task> HandleQueryCreationFailure = message => requestContext.SendError(message);


            // execute query

            //        return await QueryExecutionService.Instance.InterServiceExecuteQuery(
            //executeStringParams,
            //originConnectionInfo,
            //resultContext,
            //queryCreateSuccessFunc: null,
            //HandleQueryCreationFailure,
            //querySuccessFunction,
            //queryFailureFunction);
        }

        #endregion
    }
}
