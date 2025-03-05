//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerUtils
    {
        /// <summary>
        /// Gets the create as script for the schema
        /// </summary>
        /// <param name="schema">Schema to get the create as script for</param>
        /// <returns>Create as script for the schema</returns>
        public static List<SchemaDesignerScriptObject> GetCreateAsScriptForSchema(SchemaDesignerModel schema)
        {
            List<SchemaDesignerScriptObject> scripts = new List<SchemaDesignerScriptObject>();
            foreach (var table in schema.Tables)
            {
                SchemaDesignerScriptObject script = new SchemaDesignerScriptObject
                {
                    TableId = table.Id,
                    Script = GetCreateAsScriptForTable(table)
                };
                scripts.Add(script);
            }
            return scripts;
        }

        public static string GetCombinedScriptForSchema(SchemaDesignerModel schema)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var table in schema.Tables)
            {
                sb.AppendLine($"-- Object: {table.Name}" + "\n");
                sb.AppendLine(GetCreateAsScriptForTable(table));
                sb.AppendLine(new string('-', 80));
            }
            return sb.ToString();

        }

        /// <summary>
        /// Gets the create as script for a table
        /// </summary>
        /// <param name="table">Table to get the create as script for</param>
        /// <returns>Create as script for the table</returns>
        public static string GetCreateAsScriptForTable(SchemaDesignerTable table)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table.Name}] (");

            List<string> columnDefinitions = new List<string>();
            List<string> primaryKeys = new List<string>();
            List<string> foreignKeys = new List<string>();

            foreach (var column in table.Columns)
            {
                StringBuilder columnDef = new StringBuilder();
                columnDef.Append($"[{column.Name}] {column.DataType}");

                if (!column.IsNullable)
                    columnDef.Append(" NOT NULL");

                // TODO: Add default value
                // if (!string.IsNullOrEmpty(column.DefaultValue))
                //     columnDef.Append($" DEFAULT {column.DefaultValue}");

                if (column.IsPrimaryKey)
                    primaryKeys.Add(column.Name);

                columnDefinitions.Add(columnDef.ToString());
            }

            // Handle Primary Key constraint
            if (primaryKeys.Count > 0)
            {
                columnDefinitions.Add($"CONSTRAINT [PK_{table.Name}] PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }


            // Handle Foreign Keys
            int fkIndex = 1;
            foreach (var fk in table.ForeignKeys)
            {
                List<string> localColumns = new List<string>();
                List<string> referencedColumns = new List<string>();

                for (int i = 0; i < fk.Columns.Count; i++)
                {
                    var localColumn = fk.Columns[i];
                    var ReferencedColumn = fk.ReferencedColumns[i];
                    localColumns.Add($"[{localColumn}]");
                    referencedColumns.Add($"[{ReferencedColumn}]");
                }

                string onDelete = fk.OnDeleteAction != null ? $" ON DELETE {ConvertOnActionToSql(fk.OnDeleteAction)}" : "";
                string onUpdate = fk.OnUpdateAction != null ? $" ON UPDATE {ConvertOnActionToSql(fk.OnUpdateAction)}" : "";

                foreignKeys.Add(
                    $"CONSTRAINT [FK_{table.Name}_{fkIndex}] FOREIGN KEY ({string.Join(", ", localColumns)}) " +
                    $"REFERENCES [{fk.ReferencedTableName}] ({string.Join(", ", referencedColumns)}){onDelete}{onUpdate}"
                );
                fkIndex++;
            }

            columnDefinitions.AddRange(foreignKeys);
            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");");

            return sb.ToString();
        }

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