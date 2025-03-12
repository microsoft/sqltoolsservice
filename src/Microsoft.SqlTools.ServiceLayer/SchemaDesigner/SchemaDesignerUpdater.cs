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
        public static List<SchemaDesignerReportObject> GenerateUpdateScripts(SchemaDesignerModel initialSchema, SchemaDesignerModel updatedSchema)
        {
            var reportObjects = new List<SchemaDesignerReportObject>();
            foreach (var table in updatedSchema.Tables)
            {
                var oldTable = initialSchema.Tables.FirstOrDefault(t => t.Id == table.Id);

                if (oldTable == null)
                {
                    string creationScript = SchemaCreationScriptGenerator.GenerateTableDefinition(table);
                    reportObjects.Add(new SchemaDesignerReportObject
                    {
                        TableId = table.Id,
                        UpdateScript = creationScript,
                        TableState = SchemaDesignerReportTableState.CREATED,
                        ActionsPerformed = new List<string> { $"Table {table.Name} created." }
                    });
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
                        reportObjects.Add(new SchemaDesignerReportObject
                        {
                            TableId = oldTable.Id,
                            UpdateScript = dropForeignKeyScript,
                            TableState = SchemaDesignerReportTableState.DROPPED,
                            ActionsPerformed = new List<string> { $"Foreign key {foreignKey.Name} dropped." }
                        });
                    }
                    string dropScript = $"DROP TABLE [{oldTable.Schema}].[{oldTable.Name}]";
                    reportObjects.Add(new SchemaDesignerReportObject
                    {
                        TableId = oldTable.Id,
                        UpdateScript = dropScript,
                        TableState = SchemaDesignerReportTableState.DROPPED,
                        ActionsPerformed = new List<string> { $"Table {oldTable.Name} dropped." }
                    });
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


                    // Check for schema changes
                    if (oldTable.Schema != newTable.Schema)
                    {
                        updateScript.AppendLine($"ALTER SCHEMA [{newTable.Schema}] TRANSFER [{oldTable.Schema}].[{newTable.Name}];");
                    }

                    // Check table renaming
                    if (oldTable.Name != newTable.Name)
                    {
                        updateScript.AppendLine($"EXEC sp_rename '{oldTable.Schema}.{oldTable.Name}', '{newTable.Name}';");
                    }
                    // Table is updated
                    updateScript.AppendLine(GenerateColumnUpdateScripts(oldTable, newTable));

                    reportObjects.Add(new SchemaDesignerReportObject
                    {
                        TableId = newTable.Id,
                        UpdateScript = updateScript.ToString(),
                        TableState = SchemaDesignerReportTableState.UPDATED,
                        ActionsPerformed = new List<string> { $"Table {newTable.Name} updated." }
                    });
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
                        reportObjects.Add(new SchemaDesignerReportObject
                        {
                            TableId = table.Id,
                            UpdateScript = foreignKeyScript,
                            TableState = SchemaDesignerReportTableState.CREATED,
                            ActionsPerformed = new List<string> { $"Foreign key {foreignKey.Name} created." }
                        });
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
                            reportObjects.Add(new SchemaDesignerReportObject
                            {
                                TableId = table.Id,
                                UpdateScript = foreignKeyScript,
                                TableState = SchemaDesignerReportTableState.CREATED,
                                ActionsPerformed = new List<string> { $"Foreign key {foreignKey.Name} created." }
                            });
                        }
                        else if (!SchemaDesignerUtils.DeepCompareForeignKey(oldForeignKey, foreignKey))
                        {
                             // Foreign key exists but has changed
                            // Drop the old foreign key and create the new one
                            // Note: This is a simplified approach. In a real-world scenario, you might want to check for dependencies and handle them accordingly.

                            string dropForeignKeyScript = $"ALTER TABLE [{table.Schema}].[{table.Name}] DROP CONSTRAINT [{oldForeignKey.Name}]";
                            reportObjects.Add(new SchemaDesignerReportObject
                            {
                                TableId = table.Id,
                                UpdateScript = dropForeignKeyScript,
                                TableState = SchemaDesignerReportTableState.UPDATED,
                                ActionsPerformed = new List<string> { $"Foreign key {oldForeignKey.Name} dropped." }
                            });

                            string foreignKeyScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(table, foreignKey);
                            reportObjects.Add(new SchemaDesignerReportObject
                            {
                                TableId = table.Id,
                                UpdateScript = foreignKeyScript,
                                TableState = SchemaDesignerReportTableState.CREATED,
                                ActionsPerformed = new List<string> { $"Foreign key {foreignKey.Name} created." }
                            });
                        }
                    }

                    // Handle dropped foreign keys
                    foreach (var oldForeignKey in oldTable.ForeignKeys)
                    {
                        if (!table.ForeignKeys.Any(fk => fk.Name == oldForeignKey.Name))
                        {
                            string dropForeignKeyScript = $"ALTER TABLE [{oldTable.Schema}].[{oldTable.Name}] DROP CONSTRAINT [{oldForeignKey.Name}]";
                            reportObjects.Add(new SchemaDesignerReportObject
                            {
                                TableId = oldTable.Id,
                                UpdateScript = dropForeignKeyScript,
                                TableState = SchemaDesignerReportTableState.UPDATED,
                                ActionsPerformed = new List<string> { $"Foreign key {oldForeignKey.Name} dropped." }
                            });
                        }
                    }
                }
            }


            return reportObjects;
        }

        private static string GenerateColumnUpdateScripts(SchemaDesignerTable oldTable, SchemaDesignerTable newTable)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var newColumn in newTable.Columns)
            {
                var oldColumn = oldTable.Columns.FirstOrDefault(c => c.Name == newColumn.Name);

                if (oldColumn == null)
                {
                    // New column detected
                    sb.AppendLine($"ALTER TABLE [{newTable.Schema}].[{newTable.Name}] ADD {SchemaCreationScriptGenerator.GenerateColumnDefinition(newColumn)};");
                }
                else
                {
                    // Column exists, check for modifications
                    if (HasColumnChanged(oldColumn, newColumn))
                    {
                        sb.AppendLine($"ALTER TABLE [{newTable.Schema}].[{newTable.Name}] ALTER COLUMN {SchemaCreationScriptGenerator.GenerateColumnDefinition(newColumn)};");
                    }
                }
            }

            // Handle dropped columns
            foreach (var oldColumn in oldTable.Columns)
            {
                if (!newTable.Columns.Any(c => c.Name == oldColumn.Name))
                {
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