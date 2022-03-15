//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class OperationDTO
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }

        public OperationDTO(Operation operation)
        {
            Name = operation.Name;
            DisplayName = operation.DisplayName;
            Description = operation.Description;
            Image = operation.Image;
        }
    }
}
