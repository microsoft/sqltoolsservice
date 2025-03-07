//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerScriptGenerator
    {
        public static string GenerateCreateTableScript(SchemaDesignerModel model)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var table in model.Tables)
            {
                sb.AppendLine(GenerateTableDefinition(table));
            }

            sb.AppendLine(GenerateForeignKeyScripts(model.Tables));

            return sb.ToString();
        }

        private static string GenerateTableDefinition(SchemaDesignerTable table)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table.Schema}].[{table.Name}] (");

            List<string> columnDefinitions = new List<string>();
            foreach (var column in table.Columns)
            {
                columnDefinitions.Add(GenerateColumnDefinition(column));
            }

            // Add primary key constraint if exists
            var primaryKeyColumns = table.Columns.FindAll(c => c.IsPrimaryKey);
            if (primaryKeyColumns.Count > 0)
            {
                string primaryKeyDefinition = $"PRIMARY KEY ({string.Join(", ", primaryKeyColumns.ConvertAll(c => $"[{c.Name}]"))})";
                columnDefinitions.Add(primaryKeyDefinition);
            }

            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");");

            return sb.ToString();
        }

        public static SchemaDesignerScriptObject GenerateCreateAsScriptForTable(SchemaDesignerTable table)
        {
            return new SchemaDesignerScriptObject
            {
                TableId = table.Id,
                Script = GenerateTableDefinition(table)
            };
        }

        public static List<SchemaDesignerScriptObject> GenerateCreateAsScriptForSchemaTables(SchemaDesignerModel schema)
        {
            List<SchemaDesignerScriptObject> scripts = new List<SchemaDesignerScriptObject>();
            foreach (var table in schema.Tables)
            {
                scripts.Add(GenerateCreateAsScriptForTable(table));
            }
            return scripts;
        }

        public static string GenerateColumnDefinition(SchemaDesignerColumn column)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{column.Name}] {column.DataType}");

            if (column.MaxLength.HasValue)
                sb.Append(column.MaxLength == -1 ? "(MAX)" : $"({column.MaxLength})");

            if (column.Precision.HasValue && column.Scale.HasValue)
                sb.Append($"({column.Precision},{column.Scale})");

            if (!column.IsNullable)
                sb.Append(" NOT NULL");

            if (column.IsUnique)
                sb.Append(" UNIQUE");

            if (column.IsIdentity)
                sb.Append(" IDENTITY(1,1)");

            if (!string.IsNullOrEmpty(column.Collation))
                sb.Append($" COLLATE {column.Collation}");

            return sb.ToString();
        }

        public static string GenerateForeignKeyScripts(List<SchemaDesignerTable> tables)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var table in tables)
            {
                foreach (var fk in table.ForeignKeys)
                {
                    sb.AppendLine(GenerateForeignKeyScript(table, fk));
                }
            }

            return sb.ToString();
        }

        public static string GenerateForeignKeyScript(SchemaDesignerTable table, SchemaDesignerForeignKey fk)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"
ALTER TABLE [{table.Schema}].[{table.Name}]
ADD CONSTRAINT [{fk.Name}]
FOREIGN KEY ({fk.Columns.Select(c => $"[{c}]").Aggregate((a, b) => $"{a}, {b}")}) 
REFERENCES [{fk.ReferencedSchemaName}].[{fk.ReferencedTableName}]({fk.ReferencedColumns.Select(c => $"[{c}]").Aggregate((a, b) => $"{a}, {b}")})
ON DELETE {SchemaDesignerUtils.ConvertOnActionToSql(fk.OnDeleteAction)}
ON UPDATE {SchemaDesignerUtils.ConvertOnActionToSql(fk.OnUpdateAction)};
");
            return sb.ToString();
        }

    }
}
