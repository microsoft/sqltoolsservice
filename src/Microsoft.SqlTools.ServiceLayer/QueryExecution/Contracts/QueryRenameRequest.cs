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
    public class QueryRenameParams
    {
        public string NewOwnerUri { get; set; }
        public string OriginalOwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters to return as the result of a query dispose request
    /// </summary>
    public class QueryRenameResult
    {
        /// <summary>
        /// Any error messages that occurred during disposing the result set. Optional, can be set
        /// to null if there were no errors.
        /// </summary>
        public string Messages { get; set; }
    }

    public class QueryRenameRequest
    {
        public static readonly 
            RequestType<QueryRenameParams, QueryRenameResult> Type =
            RequestType<QueryRenameParams, QueryRenameResult>.Create("query/rename");
    }
}
