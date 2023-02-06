﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Class used for internally passing results from a cell around.
    /// </summary>
    public class DbCellValue
    {
        /// <summary>
        /// Display value for the cell, suitable to be passed back to the client
        /// </summary>
        public string DisplayValue { get; set; }

        /// <summary>
        /// Whether or not the cell is NULL
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// Culture invariant display value for the cell, this value can later be used by the client to convert back to the original value.
        /// </summary>
        public string InvariantCultureDisplayValue { get; set; }

        /// <summary>
        /// The raw object for the cell, for use internally
        /// </summary>
        internal object RawObject { get; set; }

        /// <summary>
        /// The internal ID for the row. Should be used when directly referencing the row for edit
        /// or other purposes.
        /// </summary>
        public long RowId { get; set; }

        /// <summary>
        /// Copies the values of this DbCellValue into another DbCellValue (or child object)
        /// </summary>
        /// <param name="other">The DbCellValue (or child) that will receive the values</param>
        public virtual void CopyTo(DbCellValue other)
        {
            Validate.IsNotNull(nameof(other), other);

            other.DisplayValue = DisplayValue;
            other.InvariantCultureDisplayValue = InvariantCultureDisplayValue;
            other.IsNull = IsNull;
            other.RawObject = RawObject;
            other.RowId = RowId;
        }
    }
}
