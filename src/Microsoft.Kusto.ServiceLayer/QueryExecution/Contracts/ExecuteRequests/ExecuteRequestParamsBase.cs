//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Basic parameters that are required for executing a query
    /// </summary>
    public abstract class ExecuteRequestParamsBase
    {
        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Execution plan options
        /// </summary>
        public ExecutionPlanOptions ExecutionPlanOptions { get; set; }

        /// <summary>
        /// Flag to get full column schema via additional queries.
        /// </summary>
        public bool GetFullColumnSchema { get; set; }
    }
}
