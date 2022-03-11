//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class EdgeDTO
    {
        public double EstimatedDataSize { get; set; }
        public double EstimatedRowCount { get; set; }
        public List<ExecutionPlanGraphPropertyBase> Properties { get; set; }
        public double RowCount { get; set; }
        public double RowSize { get; set; }
        public NodeDTO FromNode { get; set; }
        public NodeDTO ToNode { get; set; }

        public EdgeDTO()
        { }
    }
}
