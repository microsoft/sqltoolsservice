//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// Represents a node in the execution plan tree.
    /// </summary>
    public class NodeDTO
    {
        /// <summary>
        /// Child nodes of the node.
        /// </summary>
        public List<NodeDTO> Children { get; set; } = new List<NodeDTO>();
        /// <summary>
        /// Cost associated with the node
        /// </summary>
        public double Cost { get; set; }
        /// <summary>
        /// Description associated with the node.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Display cost for the node that appears in the UI.
        /// </summary>
        public string DisplayCost { get; set; }
        /// <summary>
        /// Display name for the node that appears in the UI.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Node edges to each of the node's children.
        /// </summary>
        public List<ExecutionPlanEdges> Edges { get; set; } = new List<ExecutionPlanEdges>();
        /// <summary>
        /// Time take by the node operation in milliseconds
        /// </summary>
        public long? ElapsedTimeInMs { get; set; }
        /// <summary>
        /// The graph the node is a part of.
        /// </summary>
        public GraphDTO Graph { get; set; }
        /// <summary>
        /// The group index for the node.
        /// </summary>
        public int GroupIndex { get; set; }
        /// <summary>
        /// Flag indicating if the node has any warnings associated with it.
        /// </summary>
        public bool HasWarnings { get; set; }
        /// <summary>
        /// ID for the node
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Flag indicating parallel execution
        /// </summary>
        public bool IsParallel { get; set; }
        /// <summary>
        /// The logical operations unlocalized name associated with the node.
        /// </summary>
        public string LogicalOpUnlocName { get; set; }
        /// <summary>
        /// Describes the operation associated with the node.
        /// </summary>
        public OperationDTO Operation { get; set; }
        /// <summary>
        /// The node's parent.
        /// </summary>
        public NodeDTO Parent { get; set; }
        /// <summary>
        /// The physical operations unlocalized name associated with the node.
        /// </summary>
        public string PhysicalOpUnlocName { get; set; }
        /// <summary>
        /// Node properties to be shown in the tooltip
        /// </summary>
        public List<ExecutionPlanGraphPropertyBase> Properties { get; set; }
        public double RelativeCost { get; set; }
        /// <summary>
        /// The root node of the execution plan tree this node is associated with.
        /// </summary>
        public NodeDTO Root { get; set; }
        /// <summary>
        /// The cost of the node's subtree.
        /// </summary>
        public double SubtreeCost { get; set; }
    }
}
