//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
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

        #region Basic Properties (properties provided by SMO)

        /// <summary>
        /// List of columns in the object being edited
        /// </summary>
        public EditColumnMetadata[] Columns { get; set; }

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
        public EditColumnMetadata[] KeyColumns { get; private set; }

        #endregion

        /// <summary>
        /// Filters out metadata that is not present in the result set, and matches metadata ordering to resultset.
        /// </summary>
        public static EditColumnMetadata[] FilterColumnMetadata(EditColumnMetadata[] metaColumns, DbColumnWrapper[] resultColumns)
        {
            if (metaColumns.Length == 0)
            {
                return metaColumns;
            }

            bool escapeColName = FromSqlScript.IsIdentifierBracketed(metaColumns[0].EscapedName);
            Dictionary<string, int> columnNameOrdinalMap = new Dictionary<string, int>(capacity: resultColumns.Length);
            for (int i = 0; i < resultColumns.Length; i++)
            {
                DbColumnWrapper column = resultColumns[i];
                string columnName =  column.ColumnName;
                if (escapeColName && !FromSqlScript.IsIdentifierBracketed(columnName))
                {
                    columnName = ToSqlScript.FormatIdentifier(columnName);
                }
                columnNameOrdinalMap.Add(columnName, column.ColumnOrdinal ?? i);
            }

            HashSet<string> resultColumnNames = columnNameOrdinalMap.Keys.ToHashSet();
            metaColumns = Array.FindAll(metaColumns, column => resultColumnNames.Contains(column.EscapedName));
            foreach (EditColumnMetadata metaCol in metaColumns)
            {
                metaCol.Ordinal = columnNameOrdinalMap[metaCol.EscapedName];
            }
            Array.Sort(metaColumns, (x, y) => (Comparer<int>.Default).Compare(x.Ordinal, y.Ordinal));

            return metaColumns;
        }

        /// <summary>
        /// Extracts extended column properties from the database columns from SQL Client
        /// </summary>
        /// <param name="dbColumnWrappers">The column information provided by SQL Client</param>
        public void Extend(DbColumnWrapper[] dbColumnWrappers)
        {
            Validate.IsNotNull(nameof(dbColumnWrappers), dbColumnWrappers);

            Columns = EditTableMetadata.FilterColumnMetadata(Columns, dbColumnWrappers);

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
