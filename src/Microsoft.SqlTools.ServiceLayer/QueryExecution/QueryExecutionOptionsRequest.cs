//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlContext;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the query execution options request
    /// </summary>
    public class QueryExecutionOptionsParams
    {
        public string OwnerUri { get; set; }

        public QueryExecutionSettings Options { get; set; }
    }

    public class QueryExecutionOptionsRequest
    {
        public static readonly
            RequestType<QueryExecutionOptionsParams, bool> Type =
                RequestType<QueryExecutionOptionsParams, bool>.Create("query/setexecutionoptions");
    }
}
