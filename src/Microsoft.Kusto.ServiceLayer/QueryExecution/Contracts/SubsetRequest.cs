//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for a query result subset retrieval request
    /// </summary>
    public class SubsetParams
    {
        /// <summary>
        /// URI for the file that owns the query to look up the results for
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// Beginning index of the rows to return from the selected resultset. This index will be
        /// included in the results.
        /// </summary>
        public long RowsStartIndex { get; set; }

        /// <summary>
        /// Number of rows to include in the result of this request. If the number of the rows 
        /// exceeds the number of rows available after the start index, all available rows after
        /// the start index will be returned.
        /// </summary>
        public int RowsCount { get; set; }
    }

    /// <summary>
    /// Parameters for the result of a subset retrieval request
    /// </summary>
    public class SubsetResult
    {
        /// <summary>
        /// The requested subset of results. Optional, can be set to null to indicate an error
        /// </summary>
        public ResultSetSubset ResultSubset { get; set; }
    }

    public class SubsetRequest
    {
        public static readonly
            RequestType<SubsetParams, SubsetResult> Type =
            RequestType<SubsetParams, SubsetResult>.Create("query/subset");
    }
}
