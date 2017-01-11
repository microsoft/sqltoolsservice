//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the query execute request
    /// </summary>
    public class QueryExecuteParams
    {
        /// <summary>
        /// The selection from the document
        /// </summary>
        public SelectionData QuerySelection { get; set; }

        /// <summary>
        /// URI for the editor that is asking for the query execute
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters for the query execute result
    /// </summary>
    public class QueryExecuteResult
    {
    }

    public class QueryExecuteRequest
    {
        public static readonly
            RequestType<QueryExecuteParams, QueryExecuteResult> Type =
            RequestType<QueryExecuteParams, QueryExecuteResult>.Create("query/execute");
    }
}
