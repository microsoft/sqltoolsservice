//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.ShowPlan;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters to be sent back with an execution plan graph event
    /// </summary>
    public class ExecutionPlanGraphEventParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Execution plan graph object.
        /// </summary>
        public ExecutionPlanGraph ExecutionPlan { get; set; }

        /// <summary>
        /// Error messages in generation of the execution plan graph
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class ExecutionPlanGraphEvent
    {
        public static readonly
            EventType<ExecutionPlanGraphEventParams> Type =
            EventType<ExecutionPlanGraphEventParams>.Create("query/executionPlanGraph");
    }
}
