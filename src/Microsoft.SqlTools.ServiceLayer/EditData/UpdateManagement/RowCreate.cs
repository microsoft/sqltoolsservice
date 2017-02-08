//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement
{
    /// <summary>
    /// Represents a row that should be added to the result set. Generates an INSERT statement.
    /// </summary>
    public sealed class RowCreate : RowEditBase
    {
        private const string InsertStatement = "INSERT INTO {0}({1}) VALUES ({2})";

        private readonly CellUpdate[] newCells;

        public RowCreate(long rowId, ResultSet associatedResultSet, IEditTableMetadata associatedMetadata)
            : base(rowId, associatedResultSet, associatedMetadata)
        {
            newCells = new CellUpdate[associatedResultSet.Columns.Length];
        }

        public override string GetScript()
        {
            List<string> columnNames = new List<string>();
            List<string> columnValues = new List<string>();

            // Build the column list and value list
            for (int i = 0; i < AssociatedResultSet.Columns.Length; i++)
            {
                DbColumnWrapper column = AssociatedResultSet.Columns[i];
                CellUpdate cell = newCells[i];

                // If the column is not updatable, then skip it
                if (!column.IsUpdatable)
                {
                    continue;
                }

                // If the cell doesn't have a value, but is updatable, don't try to create the script
                if (cell == null)
                {
                    // @TODO: Move to constants file
                    throw new InvalidOperationException("Cannot create a row with missing data");
                }

                // Add the column and the data to their respective lists
                columnNames.Add(SqlScriptFormatter.FormatIdentifier(column.ColumnName));
                columnValues.Add(SqlScriptFormatter.FormatValue(cell.Value, column));
            }

            // Put together the components of the statement
            string joinedColumnNames = string.Join(", ", columnNames);
            string joinedColumnValues = string.Join(", ", columnValues);
            return string.Format(InsertStatement, AssociatedObjectMetadata.EscapedMultipartName, joinedColumnNames,
                joinedColumnValues);
        }

        /// <summary>
        /// Sets the value of a cell in the row to be added
        /// </summary>
        /// <param name="columnId">Ordinal of the column to set in the row</param>
        /// <param name="newValue">String representation from the client of the value to add</param>
        /// <returns>
        /// The updated value as a string of the object generated from <paramref name="newValue"/>
        /// </returns>
        public override string SetCell(int columnId, string newValue)
        {
            // Validate the column and the value and convert to object
            ValidateColumnIsUpdatable(columnId);
            CellUpdate update = new CellUpdate(AssociatedResultSet.Columns[columnId], newValue);

            // Add the cell update to the 
            newCells[columnId] = update;
            return update.ValueAsString;
        }
    }
}
