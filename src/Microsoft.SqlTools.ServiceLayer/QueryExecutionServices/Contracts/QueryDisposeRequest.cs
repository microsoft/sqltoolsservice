//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts
{
    /// <summary>
    /// Parameters for the query dispose request
    /// </summary>
    public class QueryDisposeParams
    {
        public Guid QueryId { get; set; }
    }

    /// <summary>
    /// Parameters to return as the result of a query dispose request
    /// </summary>
    public class QueryDisposeResult
    {
        /// <summary>
        /// Any error messages that occurred during disposing the result set. Optional, can be set
        /// to null if there were no errors.
        /// </summary>
        public string Messages { get; set; }
    }

    public class QueryDisposeRequest
    {
        public static readonly
            RequestType<QueryDisposeParams, QueryDisposeResult> Type =
            RequestType<QueryDisposeParams, QueryDisposeResult>.Create("query/dispose");
    }
}
