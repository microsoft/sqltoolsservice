//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class SkeletonNodeDTO
    {
        public NodeDTO BaseNode { get; set; }
        public List<SkeletonNodeDTO> Children { get; set; } = new List<SkeletonNodeDTO>();
        public int GroupIndex { get; set; }
        public bool HasMatch { get; set; }
        public List<SkeletonNodeDTO> MatchingNodes { get; set; } = new List<SkeletonNodeDTO>();
        public SkeletonNodeDTO ParentNode { get; set; }
    }
}
