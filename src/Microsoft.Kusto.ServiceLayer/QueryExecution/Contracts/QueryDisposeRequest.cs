//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the query dispose request
    /// </summary>
    public class QueryDisposeParams
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters to return as the result of a query dispose request
    /// </summary>
    public class QueryDisposeResult
    {
    }

    public class QueryDisposeRequest
    {
        public static readonly
            RequestType<QueryDisposeParams, QueryDisposeResult> Type =
            RequestType<QueryDisposeParams, QueryDisposeResult>.Create("kusto/query/dispose");
    }
}
