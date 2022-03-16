//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class GraphDTO
    {
        public NodeDTO Root { get; set; }
        public DescriptionDTO Description { get; set; }
    }
}
