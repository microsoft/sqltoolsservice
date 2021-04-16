//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.Contracts.QueryExecution.ExecuteRequests
{
    /// <summary>
    /// Parameters for executing a query directly
    /// </summary>
    public class ExecuteStringParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// The query to execute
        /// </summary>
        public string Query { get; set; }
    }

    public class ExecuteStringRequest
    {
        public static readonly 
            RequestType<ExecuteStringParams, ExecuteRequestResult> Type = 
            RequestType<ExecuteStringParams, ExecuteRequestResult>.Create("query/executeString");
    }
}
