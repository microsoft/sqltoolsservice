//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// A way to return a row in a result set that is being edited. It contains state about whether
    /// or not the row is dirty
    /// </summary>
    public class EditRow
    {
        public enum EditRowState
        {
            Clean = 0,
            DirtyInsert = 1,
            DirtyDelete = 2,
            DirtyUpdate = 3
        }

        /// <summary>
        /// The cells in the row. If the row has pending changes, they will be represented in
        /// this list
        /// </summary>
        public DbCellValue[] Cells { get; set; }

        /// <summary>
        /// Whether or not the row has changes pending
        /// </summary>
        public bool IsDirty => State != EditRowState.Clean;

        /// <summary>
        /// What type of dirty state (or lack thereof) the row is
        /// </summary>
        public EditRowState State { get; set; }
    }
}
