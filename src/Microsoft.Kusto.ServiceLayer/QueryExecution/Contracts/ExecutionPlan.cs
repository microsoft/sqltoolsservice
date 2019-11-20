//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Class used to represent an execution plan from a query for transmission across JSON RPC
    /// </summary>
    public class ExecutionPlan
    {
        /// <summary>
        /// The format of the execution plan 
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// The execution plan content
        /// </summary>
        public string Content { get; set; }
    }
}
