//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters for the query execute result
    /// </summary>
    public class ExecuteRequestResult
    {
        /// <summary>
        /// Whether the query is executed in SQLCMD mode, either because it was requested, enabled in the
        /// query execution settings, or auto-detected from the query text
        /// </summary>
        public bool IsSqlCmdMode { get; set; }
    }
}
