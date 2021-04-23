//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models
{
    /// <summary>
    /// Parameters for the save results request
    /// </summary>
    public class SaveResultsRequestParams
    {
        /// <summary>
        /// The path of the file to save results in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex { get; set; }

        /// <summary>
        /// URI for the editor that called save results
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Start index of the selected rows (inclusive)
        /// </summary>
        public int? RowStartIndex { get; set; }

        /// <summary>
        /// End index of the selected rows (inclusive)
        /// </summary>
        public int? RowEndIndex { get; set; }

        /// <summary>
        /// Start index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnStartIndex { get; set; }

        /// <summary>
        /// End index of the selected columns (inclusive)
        /// </summary>
        /// <returns></returns>
        public int? ColumnEndIndex { get; set; }

        /// <summary>
        /// Check if request is a subset of result set or whole result set
        /// </summary>
        /// <returns></returns>
        public bool IsSaveSelection =>
            ColumnStartIndex.HasValue && ColumnEndIndex.HasValue
                                      && RowStartIndex.HasValue && RowEndIndex.HasValue;
    }
}