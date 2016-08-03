//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecutionServices.Contracts
{
    /// <summary>
    /// Parameters for a query result subset retrieval request
    /// </summary>
    public class QueryExecuteSubsetParams
    {
        /// <summary>
        /// ID of the query to look up the results for
        /// </summary>
        public Guid QueryId { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// Beginning index of the rows to return from the selected resultset. This index will be
        /// included in the results.
        /// </summary>
        public int RowsStartIndex { get; set; }

        /// <summary>
        /// Number of rows to include in the result of this request. If the number of the rows 
        /// exceeds the number of rows available after the start index, all available rows after
        /// the start index will be returned.
        /// </summary>
        public int RowsCount { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class QueryExecuteSubsetResult
    {
        
    }

    public class QueryExecuteSubsetRequest
    {
        public static readonly
            RequestType<QueryExecuteSubsetParams, QueryExecuteSubsetResult> Type =
            RequestType<QueryExecuteSubsetParams, QueryExecuteSubsetResult>.Create("query/subset");
    }
}
