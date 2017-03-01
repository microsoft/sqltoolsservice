//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    /// <summary>
    /// Provides metadata about the table or view being edited
    /// </summary>
    public class SmoEditTableMetadata : IEditTableMetadata
    {
        private readonly List<EditColumnWrapper> columns;
        private readonly List<EditColumnWrapper> keyColumns;

        /// <summary>
        /// Constructor that extracts useful metadata from the provided metadata objects
        /// </summary>
        /// <param name="dbColumns">DB columns from the ResultSet</param>
        /// <param name="smoObject">SMO metadata object for the table/view being edited</param>
        public SmoEditTableMetadata(IList<DbColumnWrapper> dbColumns, TableViewTableTypeBase smoObject)
        {
            Validate.IsNotNull(nameof(dbColumns), dbColumns);
            Validate.IsNotNull(nameof(smoObject), smoObject);

            // Make sure that we have equal columns on both metadata providers
            Debug.Assert(dbColumns.Count == smoObject.Columns.Count);

            // Create the columns for edit usage
            columns = new List<EditColumnWrapper>();
            for (int i = 0; i < dbColumns.Count; i++)
            {
                Column smoColumn = smoObject.Columns[i];
                DbColumnWrapper dbColumn = dbColumns[i];

                // A column is trustworthy for uniqueness if it can be updated or it has an identity
                // property. If both of these are false (eg, timestamp) we can't trust it to uniquely
                // identify a row in the table
                bool isTrustworthyForUniqueness = dbColumn.IsUpdatable || smoColumn.Identity;

                EditColumnWrapper column = new EditColumnWrapper
                {
                    DbColumn = dbColumn,
                    Ordinal = i,
                    EscapedName = SqlScriptFormatter.FormatIdentifier(dbColumn.ColumnName),
                    IsTrustworthyForUniqueness = isTrustworthyForUniqueness,

                    // A key column is determined by whether it is in the primary key and trustworthy
                    IsKey = smoColumn.InPrimaryKey && isTrustworthyForUniqueness
                };
                columns.Add(column);
            }

            // Determine what the key columns are
            keyColumns = columns.Where(c => c.IsKey).ToList();
            if (keyColumns.Count == 0)
            {
                // We didn't find any explicit key columns. Instead, we'll use all columns that are
                // trustworthy for uniqueness (usually all the columns)
                keyColumns = columns.Where(c => c.IsTrustworthyForUniqueness).ToList();
            }

            // If a table is memory optimized it is Hekaton. If it's a view, then it can't be Hekaton
            Table smoTable = smoObject as Table;
            IsMemoryOptimized = smoTable != null && smoTable.IsMemoryOptimized;

            // Escape the parts of the name
            string[] objectNameParts = {smoObject.Schema, smoObject.Name};
            EscapedMultipartName = SqlScriptFormatter.FormatMultipartIdentifier(objectNameParts);
        }

        /// <summary>
        /// Read-only list of columns in the object being edited
        /// </summary>
        public IEnumerable<EditColumnWrapper> Columns => columns.AsReadOnly();

        /// <summary>
        /// Full escaped multipart identifier for the object being edited
        /// </summary>
        public string EscapedMultipartName { get; }

        /// <summary>
        /// Whether or not the object being edited is memory optimized
        /// </summary>
        public bool IsMemoryOptimized { get; }

        /// <summary>
        /// Read-only list of columns that are used to uniquely identify a row
        /// </summary>
        public IEnumerable<EditColumnWrapper> KeyColumns => keyColumns.AsReadOnly();
    }
}
