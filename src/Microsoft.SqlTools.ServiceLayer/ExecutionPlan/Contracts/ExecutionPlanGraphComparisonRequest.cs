//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class ExecutionPlanGraphComparisonParams
    {
        /// <summary>
        /// First query execution plan for comparison.
        /// </summary>
        public ExecutionPlanGraphInfo FirstExecutionPlanGraphInfo { get; set; }

        /// <summary>
        /// Second query execution plan for comparison.
        /// </summary>
        public ExecutionPlanGraphInfo SecondExecutionPlanGraphInfo { get; set; }

        /// <summary>
        /// Flag to indicate if the database name should be ignored
        /// during comparisons.
        /// </summary>
        public bool IgnoreDatabaseName { get; set; }
    }

    public class ExecutionPlanGraphComparisonResult
    {
        /// <summary>
        /// Created ExecutionGraphComparisonResult for the first execution plan
        /// </summary>
        public ExecutionGraphComparisonResult FirstComparisonResult { get; set; }

        /// <summary>
        /// Created ExecutionGraphComparisonResult for the second execution plan
        /// </summary>
        public ExecutionGraphComparisonResult SecondComparisonResult { get; set; }
    }

    public class ExecutionPlanGraphComparisonRequest
    {
        public static readonly
            RequestType<ExecutionPlanGraphComparisonParams, ExecutionPlanGraphComparisonResult> Type =
                RequestType<ExecutionPlanGraphComparisonParams, ExecutionPlanGraphComparisonResult>.Create("executionPlan/compareExecutionPlanGraph");
    }
}
