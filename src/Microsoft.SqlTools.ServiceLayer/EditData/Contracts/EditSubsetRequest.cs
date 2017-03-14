//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Parameters for a subset retrieval request
    /// </summary>
    public class EditSubsetParams : SessionOperationParams
    {
        /// <summary>
        /// Beginning index of the rows to return from the selected resultset. This index will be
        /// included in the results.
        /// </summary>
        public long RowStartIndex { get; set; }

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
    public class EditSubsetResult
    {
        /// <summary>
        /// The number of rows returned from result set, useful for determining if less rows were
        /// returned than requested.
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// The requested subset of rows, with information about whether or not the rows are dirty
        /// </summary>
        public EditRow[] Subset { get; set; }
    }

    public class EditSubsetRequest
    {
        public static readonly
            RequestType<EditSubsetParams, EditSubsetResult> Type =
            RequestType<EditSubsetParams, EditSubsetResult>.Create("edit/subset");
    }
}