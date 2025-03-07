//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerUtils
    {
        /// <summary>
        /// Converts OnAction enum to T-SQL action text
        /// </summary>
        public static string ConvertOnActionToSql(OnAction action)
        {
            switch (action)
            {
                case OnAction.CASCADE:
                    return "CASCADE";
                case OnAction.SET_NULL:
                    return "SET NULL";
                case OnAction.SET_DEFAULT:
                    return "SET DEFAULT";
                case OnAction.NO_ACTION:
                default:
                    return "NO ACTION";
            }
        }

        /// <summary>
        /// Map the string representation of an action to the enum representation
        /// </summary>
        /// <param name="action"> The string representation of the action </param>
        /// <returns> The enum representation of the action </returns>
        public static OnAction MapOnAction(string action)
        {
            return action switch
            {
                "CASCADE" => OnAction.CASCADE,
                "NO_ACTION" => OnAction.NO_ACTION,
                "SET_NULL" => OnAction.SET_NULL,
                "SET_DEFAULT" => OnAction.SET_DEFAULT,
                _ => OnAction.NO_ACTION
            };
        }

        public static bool DeepCompareTable(SchemaDesignerTable table, SchemaDesignerTable otherTable)
        {

            return !(table == null || otherTable == null)
                && table.Name == otherTable.Name
                && table.Schema == otherTable.Schema
                && table.Columns != null && otherTable.Columns != null
                && table.Columns.Count == otherTable.Columns.Count
                && table.Columns.TrueForAll(c => otherTable.Columns.Exists(oc => DeepCompareColumn(c, oc)))
                && table.ForeignKeys != null && otherTable.ForeignKeys != null
                && table.ForeignKeys.Count == otherTable.ForeignKeys.Count
                && table.ForeignKeys.TrueForAll(fk => otherTable.ForeignKeys.Exists(ofk => DeepCompareForeignKey(fk, ofk)));
        }

        public static bool DeepCompareColumn(SchemaDesignerColumn column, SchemaDesignerColumn otherColumn)
        {
            return !(column == null || otherColumn == null)
                && column.Id == otherColumn.Id
                && column.Name == otherColumn.Name
                && column.DataType == otherColumn.DataType
                && column.IsNullable == otherColumn.IsNullable
                && column.IsPrimaryKey == otherColumn.IsPrimaryKey
                && column.IsIdentity == otherColumn.IsIdentity
                && column.IsNullable == otherColumn.IsNullable
                && column.IsUnique == otherColumn.IsUnique;
        }

        public static bool DeepCompareForeignKey(SchemaDesignerForeignKey fk, SchemaDesignerForeignKey otherFk)
        {
            return !(fk == null || otherFk == null)
                && fk.Id == otherFk.Id
                && fk.Name == otherFk.Name
                && fk.ReferencedSchemaName == otherFk.ReferencedSchemaName
                && fk.ReferencedTableName == otherFk.ReferencedTableName
                && fk.OnDeleteAction == otherFk.OnDeleteAction
                && fk.OnUpdateAction == otherFk.OnUpdateAction
                && fk.Columns != null && otherFk.Columns != null
                && fk.Columns.Count == otherFk.Columns.Count
                && fk.Columns.TrueForAll(c => otherFk.Columns.Exists(oc => c == oc))
                && fk.ReferencedColumns != null && otherFk.ReferencedColumns != null
                && fk.ReferencedColumns.Count == otherFk.ReferencedColumns.Count
                && fk.ReferencedColumns.TrueForAll(rc => otherFk.ReferencedColumns.Exists(orc => rc == orc));
        }
    }
}