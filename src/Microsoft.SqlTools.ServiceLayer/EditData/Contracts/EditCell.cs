//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Cell that wraps info from <see cref="DbCellValue"/> for edit purposes
    /// </summary>
    public class EditCell : DbCellValue
    {
        /// <summary>
        /// Default, parameterless constructor to make sure that JSON serializing is happy
        /// </summary>
        public EditCell() {}

        /// <summary>
        /// Constructs a new EditCell based on a DbCellValue
        /// </summary>
        /// <param name="dbCellValue">The DbCellValue that will be enhanced</param>
        /// <param name="isDirty">Whether or not the edit cell is dirty</param>
        public EditCell(DbCellValue dbCellValue, bool isDirty)
        {
            IsDirty = isDirty;

            DisplayValue = dbCellValue.DisplayValue;
            IsNull = dbCellValue.IsNull;
            RawObject = dbCellValue.RawObject;
        }

        /// <summary>
        /// Whether or not the cell is considered dirty
        /// </summary>
        public bool IsDirty { get; set; }
    }
}