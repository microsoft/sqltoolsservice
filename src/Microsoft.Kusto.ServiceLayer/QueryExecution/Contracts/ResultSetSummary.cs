//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Represents a summary of information about a result without returning any cells of the results
    /// </summary>
    public class ResultSetSummary
    {
        /// <summary>
        /// The ID of the result set within the batch results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the batch set within the query
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>
        /// The number of rows that are available for the resultset thus far
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// If true it indicates that all rows have been fetched and the RowCount being sent across is final for this ResultSet
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// The special action definition of the result set 
        /// </summary>
        public SpecialAction SpecialAction { get; set; }

        public override string ToString() => $"Result Summary Id:{Id}, Batch Id:'{BatchId}', RowCount:'{RowCount}', Complete:'{Complete}', SpecialAction:'{SpecialAction}'";

    }
}
