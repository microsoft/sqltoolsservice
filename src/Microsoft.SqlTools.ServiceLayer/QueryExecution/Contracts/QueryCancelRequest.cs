//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the query cancellation request
    /// </summary>
    public class QueryCancelParams
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters to return as the result of a query dispose request
    /// </summary>
    public class QueryCancelResult
    {
        /// <summary>
        /// Any error messages that occurred during disposing the result set. Optional, can be set
        /// to null if there were no errors.
        /// </summary>
        public string Messages { get; set; }
    }

    public class QueryCancelRequest
    {
        public static readonly 
            RequestType<QueryCancelParams, QueryCancelResult> Type =
            RequestType<QueryCancelParams, QueryCancelResult>.Create("query/cancel");
    }
}
