//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
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
        /// The number of rows that was returned with the resultset
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public DbColumnWrapper[] ColumnInfo { get; set; }

        /// <summary>
        /// An attached Actual Showplan in XML, if there was one
        /// </summary>
        public Batch.BatchSpecialAction SpecialAction { get; set; }

    }
}
