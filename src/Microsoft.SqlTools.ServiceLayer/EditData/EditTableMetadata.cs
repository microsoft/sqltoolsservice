//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Provides metadata about the table or view being edited
    /// </summary>
    public class EditTableMetadata
    {
        /// <summary>
        /// Constructs a simple edit table metadata provider
        /// </summary>
        public EditTableMetadata()
        {
            HasExtendedProperties = false;
        }

        /// <summary>
        /// List of columns in the object being edited
        /// </summary>
        public EditColumnMetadata[] Columns { get; set; }

        /// <summary>
        /// Full escaped multipart identifier for the object being edited
        /// </summary>
        public string EscapedMultipartName { get; set; }

        /// <summary>
        /// Whether or not the table has had extended properties added to it
        /// </summary>
        public bool HasExtendedProperties { get; private set; }

        /// <summary>
        /// Whether or not the object being edited is memory optimized
        /// </summary>
        public bool IsMemoryOptimized { get; set; }

        /// <summary>
        /// List of columns that are used to uniquely identify a row
        /// </summary>
        public EditColumnMetadata[] KeyColumns { get; private set; }

        /// <summary>
        /// Extracts extended column properties from the database columns from SQL Client
        /// </summary>
        /// <param name="dbColumnWrappers"></param>
        public void Extend(DbColumnWrapper[] dbColumnWrappers)
        {
            Validate.IsNotNull(nameof(dbColumnWrappers), dbColumnWrappers);

            // Iterate over the column wrappers and improve the columns we have
            for (int i = 0; i < Columns.Length; i++)
            {
                var editColumn = Columns[i];
                var dbColumn = dbColumnWrappers[i];

                // A column is trustworthy for uniqueness if it can be updated or it has an identity
                // property. If both of these are false (eg, timestamp) we can't trust it to uniquely
                // identify a row in the table
                editColumn.IsTrustworthyForUniqueness = dbColumn.IsUpdatable || dbColumn.IsIdentity.HasTrue();

                // A key column is determined by whether it is a key
                editColumn.IsKey = dbColumn.IsKey;

                // A column is calculated if it is identity, computed, or otherwise not updatable
                editColumn.IsCalculated = editColumn.IsIdentity || editColumn.IsComputed || !dbColumn.IsUpdatable;

                // Mark the column as extended
                editColumn.HasExtendedProperties = true;
            }

            // Determine what the key columns are
            KeyColumns = Columns.Where(c => c.IsKey.HasTrue()).ToArray();
            if (KeyColumns.Length == 0)
            {
                // We didn't find any explicit key columns. Instead, we'll use all columns that are
                // trustworthy for uniqueness (usually all the columns)
                KeyColumns = Columns.Where(c => c.IsTrustworthyForUniqueness.HasTrue()).ToArray();
            }

            // Mark that the table is now extended
            HasExtendedProperties = true;
        }
    }
}
