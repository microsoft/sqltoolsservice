// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary> 
    /// Container class for a selection range from file 
    /// </summary>
    public struct ExecutionPlanOptions
    {

        /// <summary>
        /// Settings for return the actual exection plan for a run query
        /// </summary>
        public bool IncludeActualExecutionPlan { get; set; }

        /// <summary>
        /// Settings for return the actual exection plan for a run query
        /// </summary>
        public bool IncludeEstimatedExecutionPlan { get; set; }

    }
}