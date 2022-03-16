//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// A simpler version of a node because of the reduced amount of properties.
    /// </summary>
    public class SkeletonNodeDTO
    {
        /// <summary>
        /// The base node for the skeleton.
        /// </summary>
        public NodeDTO BaseNode { get; set; }
        /// <summary>
        /// The children of the skeleton node.
        /// </summary>
        public List<SkeletonNodeDTO> Children { get; set; } = new List<SkeletonNodeDTO>();
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
        public List<SkeletonNodeDTO> MatchingNodes { get; set; } = new List<SkeletonNodeDTO>();
        /// <summary>
        /// The parent of the skeleton node.
        /// </summary>
        public SkeletonNodeDTO ParentNode { get; set; }
    }
}
