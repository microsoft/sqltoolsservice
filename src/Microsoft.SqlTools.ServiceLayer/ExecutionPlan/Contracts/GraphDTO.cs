//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class GraphDTO
    {
        /// <summary>
        /// The root node where the execution tree/graph starts.
        /// </summary>
        public NodeDTO Root { get; set; }
        /// <summary>
        /// Description that details the graph.
        /// </summary>
        public DescriptionDTO Description { get; set; }
    }
}
