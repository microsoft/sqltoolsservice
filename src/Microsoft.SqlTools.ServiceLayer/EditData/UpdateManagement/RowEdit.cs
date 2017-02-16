//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.EditData.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Base class for row edit operations. Provides basic information and helper functionality
    /// that all RowEdit implementations can use. Defines functionality that must be implemented
    /// in all child classes.
    /// </summary>
    public abstract class RowEditBase
    {
        /// <summary>
        /// Internal parameterless constructor, required for mocking
        /// </summary>
        protected internal RowEditBase() { }

        /// <summary>
        /// Base constructor for a row edit. Stores the state that should be available to all row
        /// edit implementations.
        /// </summary>
        /// <param name="rowId">The internal ID of the row that is being edited</param>
        /// <param name="associatedResultSet">The result set that will be updated</param>
        /// <param name="associatedMetadata">Metadata provider for the object to edit</param>
        protected RowEditBase(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
        {
            RowId = rowId;
            AssociatedResultSet = associatedResultSet;
            AssociatedObjectMetadata = associatedMetadata;
        }

        #region Properties

        /// <summary>
        /// The internal ID of the row to which this edit applies, relative to the result set
        /// </summary>
        public long RowId { get; }

        /// <summary>
        /// The result set that is associated with this row edit
        /// </summary>
        public ResultSet AssociatedResultSet { get; }

        public IEditTableMetadata AssociatedObjectMetadata { get; }

        #endregion

        /// <summary>
        /// Converts the row edit into a SQL statement
        /// </summary>
        /// <returns>A SQL statement</returns>
        public abstract string GetScript();

        /// <summary>
        /// Changes the value a cell in the row.
        /// </summary>
        /// <param name="columnId">Ordinal of the column in the row to update</param>
        /// <param name="newValue">The new value for the cell</param>
        /// <returns>The value of the cell after applying validation logic</returns>
        public abstract EditUpdateCellResult SetCell(int columnId, string newValue);

        /// <summary>
        /// Performs validation of column ID and if column can be updated.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="columnId"/> is less than 0 or greater than the number of columns
        /// in the row
        /// </exception>
        /// <exception cref="InvalidOperationException">If the column is not updatable</exception>
        /// <param name="columnId">Ordinal of the column to update</param>
        protected void ValidateColumnIsUpdatable(int columnId)
        {
            // Sanity check that the column ID is within the range of columns
            if (columnId >= AssociatedResultSet.Columns.Length || columnId < 0)
            {
                // @TODO: Add to constants file
                throw new ArgumentOutOfRangeException(nameof(columnId), "Column ID must be in the range of columns for the query");
            }

            DbColumnWrapper column = AssociatedResultSet.Columns[columnId];
            if (!column.IsUpdatable)
            {
                // @TODO: Add to constants file
                throw new InvalidOperationException("Column cannot be edited");
            }
        }

        protected WhereClause GetWhereClause(bool parameterize)
        {
            WhereClause output = new WhereClause();

            if (!AssociatedObjectMetadata.KeyColumns.Any())
            {
                // @TODO Move to constants file
                throw new InvalidOperationException("No key columns were found");
            }

            IList<DbCellValue> row = AssociatedResultSet.GetRow(RowId);
            foreach (IEditColumnWrapper col in AssociatedObjectMetadata.KeyColumns)
            {
                // Put together a clause for the value of the cell
                DbCellValue cellData = row[col.Ordinal];
                string cellDataClause;
                if (cellData.IsNull)
                {
                    cellDataClause = "IS NULL";
                }
                else
                {
                    if (cellData.RawObject is byte[] ||
                        col.DbColumn.DataTypeName.Equals("TEXT", StringComparison.OrdinalIgnoreCase) ||
                        col.DbColumn.DataTypeName.Equals("NTEXT", StringComparison.OrdinalIgnoreCase))
                    {
                        // Special cases for byte[] and TEXT/NTEXT types
                        cellDataClause = "IS NOT NULL";
                    }
                    else
                    {
                        // General case is to just use the value from the cell
                        if (parameterize)
                        {
                            // Add a parameter and parameterized clause component
                            // NOTE: We include the row ID to make sure the parameter is unique if
                            //       we execute multiple row edits at once.
                            string paramName = $"@Param{RowId}{col.Ordinal}";
                            cellDataClause = $"= {paramName}";
                            output.Parameters.Add(new SqlParameter(paramName, col.DbColumn.SqlDbType));
                        }
                        else
                        {
                            // Add the clause component with the formatted value
                            cellDataClause = $"= {SqlScriptFormatter.FormatValue(cellData, col.DbColumn)}";
                        }
                    }
                }

                string completeComponent = $"({col.EscapedName} {cellDataClause})";
                output.ClauseComponents.Add(completeComponent);
            }

            return output;
        }

        protected class WhereClause
        {
            public WhereClause()
            {
                Parameters = new List<DbParameter>();
                ClauseComponents = new List<string>();
            }

            public List<DbParameter> Parameters { get; }

            public List<string> ClauseComponents { get; }

            public string CommandText => $"WHERE {string.Join(" AND ", ClauseComponents)}";
        }
    }
}
