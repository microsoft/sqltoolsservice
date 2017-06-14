//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters for executing a query from a document open in the workspace
    /// </summary>
    public class ExecuteAndReturnResultParams
    {
        /// <summary>
        /// The string to execute
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// The owneruri to get connection from
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Result
    /// </summary>
    public class ExecuteAndReturnResultResult
    {

        /// <summary>
        /// The number of rows that was returned with the resultset
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// 2D array of the cell values requested from result set
        /// </summary>
        public DbCellValue[][] Rows { get; set; }
    }

    public class ExecuteAndReturnResultRequest
    {
        public static readonly
            RequestType<ExecuteAndReturnResultParams, ExecuteAndReturnResultResult> Type =
            RequestType<ExecuteAndReturnResultParams, ExecuteAndReturnResultResult>.Create("query/executeandreturnresult");
    }
}
