//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.ShowPlanGraph.Comparison;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class SkeletonNodeDTO
    {
        public NodeDTO BaseNode { get; set; }
        public IList<SkeletonNodeDTO> Children { get; set; }
        public int GroupIndex { get; set; }
        public bool HasMatch { get; set; }
        public List<SkeletonNodeDTO> MatchingNodes { get; set; }
        public SkeletonNodeDTO ParentNode { get; set; }
        
        public SkeletonNodeDTO()
        { }
    }
}