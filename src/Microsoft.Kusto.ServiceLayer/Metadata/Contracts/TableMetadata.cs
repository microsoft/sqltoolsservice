//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Metadata
{
    /// <summary>
    /// Provides metadata about the table or view being edited
    /// </summary>
    public class TableMetadata
    {
        /// <summary>
        /// Constructs a simple edit table metadata provider
        /// </summary>
        public TableMetadata()
        {
            HasExtendedProperties = false;
        }

        #region Basic Properties (properties provided by SMO)

        /// <summary>
        /// List of columns in the object being edited
        /// </summary>
        public ColumnMetadata[] Columns { get; set; }

        /// <summary>
        /// Full escaped multipart identifier for the object being edited
        /// </summary>
        public string EscapedMultipartName { get; set; }

        /// <summary>
        /// Whether or not the object being edited is memory optimized
        /// </summary>
        public bool IsMemoryOptimized { get; set; }

        #endregion

        #region Extended Properties (properties provided by SqlClient)

        /// <summary>
        /// Whether or not the table has had extended properties added to it
        /// </summary>
        public bool HasExtendedProperties { get; private set; }

        /// <summary>
        /// List of columns that are used to uniquely identify a row
        /// </summary>
        public ColumnMetadata[] KeyColumns { get; private set; }

        #endregion

        /// <summary>
        /// Extracts extended column properties from the database columns from SQL Client
        /// </summary>
        /// <param name="dbColumnWrappers">The column information provided by SQL Client</param>
        public void Extend(DbColumnWrapper[] dbColumnWrappers)
        {
            Validate.IsNotNull(nameof(dbColumnWrappers), dbColumnWrappers);

            // Iterate over the column wrappers and improve the columns we have
            for (int i = 0; i < Columns.Length; i++)
            {
                Columns[i].Extend(dbColumnWrappers[i]);
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
