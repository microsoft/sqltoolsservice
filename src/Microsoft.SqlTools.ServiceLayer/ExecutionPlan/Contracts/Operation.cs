//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    /// <summary>
    /// Describes an operation in the execution plan.
    /// </summary>
    public class Operation
    {
        /// <summary>
        /// Name of the operation.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Display name for the operation.
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Description for the operation.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Icon/Image associated with the operation.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Instantiates an operation DTO
        /// </summary>
        /// <param name="operation">Operation object that will be used to create the DTO.</param>
        public Operation(ExecutionGraph.Operation operation)
        {
            Name = operation.Name;
            DisplayName = operation.DisplayName;
            Description = operation.Description;
            Image = operation.Image;
        }
    }
}
