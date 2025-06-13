//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Provides utility methods for the Schema Designer functionality.
    /// </summary>
    public static class SchemaDesignerUtils
    {
        /// <summary>
        /// Converts OnAction enum to T-SQL action text.
        /// </summary>
        /// <param name="action">The action to convert.</param>
        /// <returns>The SQL representation of the action.</returns>
        public static string ConvertOnActionToSql(OnAction action)
        {
            return action switch
            {
                OnAction.CASCADE => "CASCADE",
                OnAction.NO_ACTION => "NO ACTION",
                OnAction.SET_NULL => "SET NULL",
                OnAction.SET_DEFAULT => "SET DEFAULT",
                _ => "NO ACTION"
            };
        }

        public static SqlForeignKeyAction ConvertOnActionToSqlForeignKeyAction(OnAction action)
        {
            return action switch
            {
                OnAction.CASCADE => SqlForeignKeyAction.Cascade,
                OnAction.NO_ACTION => SqlForeignKeyAction.NoAction,
                OnAction.SET_NULL => SqlForeignKeyAction.SetNull,
                OnAction.SET_DEFAULT => SqlForeignKeyAction.SetDefault,
                _ => SqlForeignKeyAction.NoAction
            };
        }

        public static OnAction ConvertSqlForeignKeyActionToOnAction(SqlForeignKeyAction action)
        {
            return action switch
            {
                SqlForeignKeyAction.Cascade => OnAction.CASCADE,
                SqlForeignKeyAction.NoAction => OnAction.NO_ACTION,
                SqlForeignKeyAction.SetNull => OnAction.SET_NULL,
                SqlForeignKeyAction.SetDefault => OnAction.SET_DEFAULT,
                _ => OnAction.NO_ACTION
            };
        }

        /// <summary>
        /// Maps the string representation of an action to the enum representation.
        /// </summary>
        /// <param name="action">The string representation of the action.</param>
        /// <returns>The enum representation of the action.</returns>
        public static OnAction MapOnAction(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return OnAction.NO_ACTION;
            }

            return action.ToUpperInvariant() switch
            {
                "CASCADE" => OnAction.CASCADE,
                "NO_ACTION" => OnAction.NO_ACTION,
                "SET_NULL" => OnAction.SET_NULL,
                "SET_DEFAULT" => OnAction.SET_DEFAULT,
                _ => OnAction.NO_ACTION
            };
        }

        public static OnAction MapForeignKeyActionToOnAction(ForeignKeyAction action)
        {
            return action switch
            {
                ForeignKeyAction.Cascade => OnAction.CASCADE,
                ForeignKeyAction.NoAction => OnAction.NO_ACTION,
                ForeignKeyAction.SetNull => OnAction.SET_NULL,
                ForeignKeyAction.SetDefault => OnAction.SET_DEFAULT,
                _ => OnAction.NO_ACTION
            };
        }

        /// <summary>
        /// Performs a deep comparison of two table definitions.
        /// </summary>
        /// <param name="table">The first table to compare.</param>
        /// <param name="otherTable">The second table to compare.</param>
        /// <returns>True if the tables are identical; otherwise, false.</returns>
        public static bool DeepCompareTable(SchemaDesignerTable table, SchemaDesignerTable otherTable)
        {
            if (table == null || otherTable == null)
            {
                return false;
            }

            // Compare basic properties
            if (table.Name != otherTable.Name ||
                table.Schema != otherTable.Schema)
            {
                return false;
            }

            // Validate columns collections
            if (table.Columns == null || otherTable.Columns == null)
            {
                return table.Columns == otherTable.Columns; // Both should be null to be equal
            }

            // Compare column counts
            if (table.Columns.Count != otherTable.Columns.Count)
            {
                return false;
            }

            // Ensure every column in the first table has a matching column in the second table
            foreach (var column in table.Columns)
            {
                // Find matching column by ID
                var matchingColumn = otherTable.Columns.Find(c => c.Id == column.Id);

                // If no matching column found or columns are different, tables are not equal
                if (matchingColumn == null || !DeepCompareColumn(column, matchingColumn))
                {
                    return false;
                }
            }

            if (table.Name != otherTable.Name ||
               table.Schema != otherTable.Schema ||
               table.Columns == null ||
               otherTable.Columns == null ||
               table.Columns.Count != otherTable.Columns.Count)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Performs a deep comparison of two column definitions.
        /// </summary>
        /// <param name="column">The first column to compare.</param>
        /// <param name="otherColumn">The second column to compare.</param>
        /// <returns>True if the columns are identical; otherwise, false.</returns>
        public static bool DeepCompareColumn(SchemaDesignerColumn column, SchemaDesignerColumn otherColumn)
        {
            if (column == null || otherColumn == null)
            {
                return false;
            }

            return column.Id == otherColumn.Id &&
                column.Name == otherColumn.Name &&
                column.DataType == otherColumn.DataType &&
                column.IsNullable == otherColumn.IsNullable &&
                column.IsPrimaryKey == otherColumn.IsPrimaryKey &&
                column.IsIdentity == otherColumn.IsIdentity &&
                column.IdentitySeed == otherColumn.IdentitySeed &&
                column.IdentityIncrement == otherColumn.IdentityIncrement &&
                column.MaxLength == otherColumn.MaxLength &&
                column.Precision == otherColumn.Precision &&
                column.Scale == otherColumn.Scale;

        }


        /// <summary>
        /// Performs a deep comparison of two foreign key definitions.
        /// </summary>
        /// <param name="fk">The first foreign key to compare.</param>
        /// <param name="otherFk">The second foreign key to compare.</param>
        /// <returns>True if the foreign keys are identical; otherwise, false.</returns>
        public static bool DeepCompareForeignKey(SchemaDesignerForeignKey fk, SchemaDesignerForeignKey otherFk)
        {
            if (fk == null || otherFk == null)
            {
                return false;
            }

            // Compare basic properties
            if (fk.Id != otherFk.Id ||
                fk.Name != otherFk.Name ||
                !string.Equals(fk.ReferencedSchemaName, otherFk.ReferencedSchemaName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(fk.ReferencedTableName, otherFk.ReferencedTableName, StringComparison.OrdinalIgnoreCase) ||
                fk.OnDeleteAction != otherFk.OnDeleteAction ||
                fk.OnUpdateAction != otherFk.OnUpdateAction)
            {
                return false;
            }


            // Validate column collections
            if (fk.Columns == null || otherFk.Columns == null ||
                fk.ReferencedColumns == null || otherFk.ReferencedColumns == null)
            {
                // Both should be null to be equal
                return (fk.Columns == otherFk.Columns) && (fk.ReferencedColumns == otherFk.ReferencedColumns);
            }

            // Compare column counts
            if (fk.Columns.Count != otherFk.Columns.Count ||
                fk.ReferencedColumns.Count != otherFk.ReferencedColumns.Count)
            {
                return false;
            }


            // Compare columns (order matters in foreign keys)
            for (int i = 0; i < fk.Columns.Count; i++)
            {
                if (!string.Equals(fk.Columns[i], otherFk.Columns[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Compare referenced columns (order matters in foreign keys)
            for (int i = 0; i < fk.ReferencedColumns.Count; i++)
            {
                if (!string.Equals(fk.ReferencedColumns[i], otherFk.ReferencedColumns[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}