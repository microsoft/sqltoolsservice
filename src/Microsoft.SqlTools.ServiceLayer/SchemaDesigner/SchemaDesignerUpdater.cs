//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerUpdater
    {
        public static void AddOrRemoveReport(Dictionary<string, SchemaDesignerReportObject> report, SchemaDesignerTable table, SchemaDesignerReportTableState type, string script, string actionPerformed)
        {
            if (report.ContainsKey(table.Id.ToString()))
            {
                report[table.Id.ToString()].ActionsPerformed.Add(actionPerformed);
                report[table.Id.ToString()].UpdateScript += script;
            }
            else
            {
                report.Add(table.Id.ToString(), new SchemaDesignerReportObject
                {
                    TableId = table.Id,
                    TableName = $"{table.Schema}.{table.Name}",
                    UpdateScript = script,
                    TableState = type,
                    ActionsPerformed = new List<string> { actionPerformed }
                });
            }
        }

        public static GetReportResponse GenerateUpdateScripts(SchemaDesignerModel initialSchema, SchemaDesignerModel updatedSchema)
        {
            Dictionary<string, SchemaDesignerReportObject> report = new Dictionary<string, SchemaDesignerReportObject>();
            StringBuilder updateScriptResult = new StringBuilder();
            foreach (var table in updatedSchema.Tables)
            {
                var oldTable = initialSchema.Tables.FirstOrDefault(t => t.Id == table.Id);

                if (oldTable == null)
                {
                    string creationScript = SchemaCreationScriptGenerator.GenerateTableDefinition(table);
                    updateScriptResult.AppendLine(creationScript);
                    AddOrRemoveReport(report, table, SchemaDesignerReportTableState.CREATED, creationScript, $"Table {table.Name} created.");
                }
            }

            

            // Handle updated tables
            foreach (var oldTable in initialSchema.Tables)
            {
                StringBuilder updateScript = new StringBuilder();
                var newTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == oldTable.Id);

                if (newTable != null)
                {
                    if (SchemaDesignerUtils.DeepCompareTable(oldTable, newTable))
                    {
                        // No changes detected
                        continue;
                    }

                    // Check table renaming
                    if (oldTable.Name != newTable.Name)
                    {
                        AddOrRemoveReport(report, newTable, SchemaDesignerReportTableState.UPDATED, "", $"Table {oldTable.Name} renamed to {newTable.Name}.");
                        
                        updateScript.AppendLine($"EXEC sp_rename '{oldTable.Schema}.{oldTable.Name}', '{newTable.Name}';");
                    }

                    // Check for schema changes
                    if (oldTable.Schema != newTable.Schema)
                    {
                        AddOrRemoveReport(report, newTable, SchemaDesignerReportTableState.UPDATED, "", $"Table {newTable.Name} schema changed.");
                        updateScript.AppendLine($"ALTER SCHEMA [{newTable.Schema}] TRANSFER [{oldTable.Schema}].[{newTable.Name}];");
                    }

                    // Table is updated
                    updateScript.AppendLine(GenerateColumnUpdateScripts(oldTable, newTable, report));
                    AddOrRemoveReport(report, newTable, SchemaDesignerReportTableState.UPDATED, updateScript.ToString(), $"Table {newTable.Name} updated.");
                    updateScriptResult.AppendLine(updateScript.ToString());
                }
            }

            // Handle foreign key updates for new tables
            foreach (var table in updatedSchema.Tables)
            {
                var oldTable = initialSchema.Tables.FirstOrDefault(t => t.Id == table.Id);

                if (oldTable == null)
                {
                    // New table detected, generate foreign key scripts for it
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        string foreignKeyScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(table, foreignKey);
                        AddOrRemoveReport(report, table, SchemaDesignerReportTableState.CREATED, foreignKeyScript, $"Foreign key {foreignKey.Name} created.");
                        updateScriptResult.AppendLine(foreignKeyScript);
                    }
                }
            }

            // Handle foreign key updates for existing tables
            foreach (var table in updatedSchema.Tables)
            {
                var oldTable = initialSchema.Tables.FirstOrDefault(t => t.Id == table.Id);

                if (oldTable != null)
                {
                    // Check for foreign key changes
                    foreach (var foreignKey in table.ForeignKeys)
                    {
                        var oldForeignKey = oldTable.ForeignKeys.FirstOrDefault(fk => fk.Name == foreignKey.Name);
                        if (oldForeignKey == null)
                        {
                            string foreignKeyScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(table, foreignKey);
                            AddOrRemoveReport(report, table, SchemaDesignerReportTableState.CREATED, foreignKeyScript, $"Foreign key {foreignKey.Name} created.");
                            updateScriptResult.AppendLine(foreignKeyScript);
                        }
                        else if (!SchemaDesignerUtils.DeepCompareForeignKey(oldForeignKey, foreignKey))
                        {
                            // Foreign key exists but has changed
                            // Drop the old foreign key and create the new one
                            // Note: This is a simplified approach. In a real-world scenario, you might want to check for dependencies and handle them accordingly.

                            string dropForeignKeyScript = $"ALTER TABLE [{table.Schema}].[{table.Name}] DROP CONSTRAINT [{oldForeignKey.Name}]";
                            AddOrRemoveReport(report, table, SchemaDesignerReportTableState.UPDATED, dropForeignKeyScript, $"Foreign key {oldForeignKey.Name} dropped.");
                            updateScriptResult.AppendLine(dropForeignKeyScript);

                            string foreignKeyScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(table, foreignKey);
                            AddOrRemoveReport(report, table, SchemaDesignerReportTableState.CREATED, foreignKeyScript, $"Foreign key {foreignKey.Name} created.");
                            updateScriptResult.AppendLine(foreignKeyScript);
                        }
                    }

                    // Handle dropped foreign keys
                    foreach (var oldForeignKey in oldTable.ForeignKeys)
                    {
                        if (!table.ForeignKeys.Any(fk => fk.Name == oldForeignKey.Name))
                        {
                            string dropForeignKeyScript = $"ALTER TABLE [{oldTable.Schema}].[{oldTable.Name}] DROP CONSTRAINT [{oldForeignKey.Name}]";
                            AddOrRemoveReport(report, oldTable, SchemaDesignerReportTableState.UPDATED, dropForeignKeyScript, $"Foreign key {oldForeignKey.Name} dropped.");
                            updateScriptResult.AppendLine(dropForeignKeyScript);
                        }
                    }
                }
            }

            // Handle dropped tables
            foreach (var oldTable in initialSchema.Tables)
            {
                var newTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == oldTable.Id);

                if (newTable == null)
                {
                    // Drop foreign keys first
                    foreach (var foreignKey in oldTable.ForeignKeys)
                    {
                        string dropForeignKeyScript = "ALTER TABLE [{oldTable.Schema}].[{oldTable.Name}] DROP CONSTRAINT [{foreignKey.Name}]";
                        AddOrRemoveReport(report, oldTable, SchemaDesignerReportTableState.DROPPED, dropForeignKeyScript, $"Foreign key {foreignKey.Name} dropped.");
                        updateScriptResult.AppendLine(dropForeignKeyScript);
                    }
                    string dropScript = $"DROP TABLE [{oldTable.Schema}].[{oldTable.Name}]";
                    AddOrRemoveReport(report, oldTable, SchemaDesignerReportTableState.DROPPED, dropScript, $"Table {oldTable.Name} dropped.");
                    updateScriptResult.AppendLine(dropScript);
                }
            }

            // Convert the report dictionary to a list of SchemaDesignerReportObject
            List<SchemaDesignerReportObject> reportObjects = report.Values.ToList();

            return new GetReportResponse() {
                Reports = reportObjects,
                UpdateScript = updateScriptResult.ToString()
            };
        }

        private static string GenerateColumnUpdateScripts(SchemaDesignerTable oldTable, SchemaDesignerTable newTable, Dictionary<string, SchemaDesignerReportObject> report)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var newColumn in newTable.Columns)
            {
                var oldColumn = oldTable.Columns.FirstOrDefault(c => c.Name == newColumn.Name);

                if (oldColumn == null)
                {
                    AddOrRemoveReport(report, newTable, SchemaDesignerReportTableState.UPDATED, "", $"Column {newColumn.Name} added.");
                    // New column detected
                    sb.AppendLine($"ALTER TABLE [{newTable.Schema}].[{newTable.Name}] ADD {SchemaCreationScriptGenerator.GenerateColumnDefinition(newColumn)};");
                }
                else
                {
                    // Column exists, check for modifications
                    if (HasColumnChanged(oldColumn, newColumn))
                    {
                        AddOrRemoveReport(report, newTable, SchemaDesignerReportTableState.UPDATED, "", $"Column {newColumn.Name} updated.");
                        sb.AppendLine($"ALTER TABLE [{newTable.Schema}].[{newTable.Name}] ALTER COLUMN {SchemaCreationScriptGenerator.GenerateColumnDefinition(newColumn)};");
                    }
                }
            }

            // Handle dropped columns
            foreach (var oldColumn in oldTable.Columns)
            {
                if (!newTable.Columns.Any(c => c.Name == oldColumn.Name))
                {
                    AddOrRemoveReport(report, oldTable, SchemaDesignerReportTableState.UPDATED, "", $"Column {oldColumn.Name} dropped.");
                    sb.AppendLine($"ALTER TABLE [{oldTable.Schema}].[{oldTable.Name}] DROP COLUMN [{oldColumn.Name}];");
                }
            }

            return sb.ToString();
        }

        private static bool HasColumnChanged(SchemaDesignerColumn oldColumn, SchemaDesignerColumn newColumn)
        {
            return oldColumn.DataType != newColumn.DataType ||
                oldColumn.MaxLength != newColumn.MaxLength ||
                oldColumn.Precision != newColumn.Precision ||
                oldColumn.Scale != newColumn.Scale ||
                oldColumn.IsNullable != newColumn.IsNullable ||
                oldColumn.IsPrimaryKey != newColumn.IsPrimaryKey ||
                oldColumn.IsIdentity != newColumn.IsIdentity ||
                oldColumn.IdentitySeed != newColumn.IdentitySeed ||
                oldColumn.IdentityIncrement != newColumn.IdentityIncrement ||
                oldColumn.IsUnique != newColumn.IsUnique ||
                oldColumn.Collation != newColumn.Collation;
        }
    }
}