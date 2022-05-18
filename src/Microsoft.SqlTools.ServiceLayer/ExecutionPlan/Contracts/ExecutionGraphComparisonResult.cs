//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// An ExecutionGraphComparisonResult is composed of an execution plan node, but has additional properties
    /// to keep track of matching ExecutionGraphComparisonResult nodes for execution plan nodes present in the
    /// the graph being compared against. This class also features a group index that can assist
    /// with coloring similar sections of execution plans in the UI.
    /// </summary>
    public class ExecutionGraphComparisonResult
    {
        /// <summary>
        /// The base ExecutionPlanNode for the ExecutionGraphComparisonResult.
        /// </summary>
        public ExecutionPlanNode BaseNode { get; set; }
        /// <summary>
        /// The children of the ExecutionGraphComparisonResult.
        /// </summary>
        public List<ExecutionGraphComparisonResult> Children { get; set; } = new List<ExecutionGraphComparisonResult>();
        /// <summary>
        /// The group index of the ExecutionGraphComparisonResult.
        /// </summary>
        public int GroupIndex { get; set; }
        /// <summary>
        /// Flag to indicate if the ExecutionGraphComparisonResult has a matching node in the compared execution plan.
        /// </summary>
        public bool HasMatch { get; set; }
        /// <summary>
        /// List of matching nodes for the ExecutionGraphComparisonResult.
        /// </summary>
        public List<int> MatchingNodesId { get; set; } = new List<int>();
        /// <summary>
        /// The parent of the ExecutionGraphComparisonResult.
        /// </summary>
        public ExecutionGraphComparisonResult ParentNode { get; set; }
    }
}
