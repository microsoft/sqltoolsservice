//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for query execution plan request
    /// </summary>
    public class QueryExecutionPlanParams
    {
        /// <summary>
        /// URI for the file that owns the query to look up the results for
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

    }

    /// <summary>
    /// Parameters for the query execution plan request
    /// </summary>
    public class QueryExecutionPlanResult
    {
        /// <summary>
        /// The requested execution plan. Optional, can be set to null to indicate an error
        /// </summary>
        public ExecutionPlan ExecutionPlan { get; set; }
    }

    public class QueryExecutionPlanRequest
    {
        public static readonly
            RequestType<QueryExecutionPlanParams, QueryExecutionPlanResult> Type =
            RequestType<QueryExecutionPlanParams, QueryExecutionPlanResult>.Create("query/executionPlan");
    }
}
