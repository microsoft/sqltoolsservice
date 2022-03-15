//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class DescriptionDTO
    {
        public string Title { get; set; }
        public string QueryText { get; set; }
        public string ClusteredMode { get; set; }
        public bool IsClusteredMode { get; set; }
        public List<MissingIndex> MissingIndices { get; set; }

        public DescriptionDTO()
        { }

        public DescriptionDTO(Description description)
        {
            Title = description.Title;
            QueryText = description.QueryText;
            ClusteredMode = description.ClusteredMode;
            IsClusteredMode = description.IsClusteredMode;
            MissingIndices = description.MissingIndices;
        }
    }
}
