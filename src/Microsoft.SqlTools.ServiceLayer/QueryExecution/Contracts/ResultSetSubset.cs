//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Class used to represent a subset of results from a query for transmission across JSON RPC
    /// </summary>
    public class ResultSetSubset
    {
        /// <summary>
        /// The number of rows returned from result set, useful for determining if less rows were
        /// returned than requested.
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 2D array of the cell values requested from result set
        /// </summary>
        public string[][] Rows { get; set; }
    }
}
