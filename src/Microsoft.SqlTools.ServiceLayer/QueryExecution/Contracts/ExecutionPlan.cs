//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Class used to represent a subset of results from a query for transmission across JSON RPC
    /// </summary>
    public class ExecutionPlan
    {
        /// <summary>
        /// The format of the 
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Content { get; set; }
    }
}