//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// A SkeletonNode is composed of an execution plan node, but has additional properties
    /// to keep track of matching skeleton nodes for execution plan nodes present in the
    /// the graph being compared against. This class also features a group index that can assist
    /// with coloring similar sections of execution plans in the UI.
    /// </summary>
    public class SkeletonNode
    {
        /// <summary>
        /// The base node for the skeleton.
        /// </summary>
        public ExecutionPlanNode BaseNode { get; set; }
        /// <summary>
        /// The children of the skeleton node.
        /// </summary>
        public List<SkeletonNode> Children { get; set; } = new List<SkeletonNode>();
        /// <summary>
        /// The group index of the skeleton node.
        /// </summary>
        public int GroupIndex { get; set; }
        /// <summary>
        /// Flag to indicate if the skeleton node has a matching node in the compared skeleton.
        /// </summary>
        public bool HasMatch { get; set; }
        /// <summary>
        /// List of matching nodes for the skeleton node.
        /// </summary>
        public List<SkeletonNode> MatchingNodes { get; set; } = new List<SkeletonNode>();
        /// <summary>
        /// The parent of the skeleton node.
        /// </summary>
        public SkeletonNode ParentNode { get; set; }
    }
}
