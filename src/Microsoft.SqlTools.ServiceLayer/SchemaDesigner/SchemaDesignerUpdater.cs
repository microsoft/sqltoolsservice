//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using DacSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;


namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    /// <summary>
    /// Provides functionality for updating database schemas and generating update scripts.
    /// </summary>
    public static class SchemaDesignerUpdater
    {
        /// <summary>
        /// Adds or updates a report entry for a table modification.
        /// </summary>
        /// <param name="reportDictionary">The report dictionary to update.</param>
        /// <param name="table">The table being modified.</param>
        /// <param name="tableState">The type of table modification.</param>
        /// <param name="sqlScript">The SQL script for the modification.</param>
        /// <param name="changeDescription">A description of the action performed.</param>
        public static void TrackTableChange(
            Dictionary<string, SchemaDesignerReportObject> reportDictionary,
            SchemaDesignerTable table,
            SchemaDesignerReportTableState tableState,
            string sqlScript,
            string changeDescription)
        {
            string tableId = table.Id.ToString();

            if (reportDictionary.TryGetValue(tableId, out SchemaDesignerReportObject? existingReport))
            {
                if (tableState > existingReport.TableState)
                {
                    existingReport.TableState = tableState;
                }
            }

            if (reportDictionary.ContainsKey(table.Id.ToString()))
            {
                reportDictionary[table.Id.ToString()].ActionsPerformed.Add(changeDescription);
                reportDictionary[table.Id.ToString()].UpdateScript += sqlScript;
            }
            else
            {
                reportDictionary.Add(table.Id.ToString(), new SchemaDesignerReportObject
                {
                    TableId = table.Id,
                    TableName = $"{table.Schema}.{table.Name}",
                    UpdateScript = sqlScript,
                    TableState = tableState,
                    ActionsPerformed = new List<string> { changeDescription }
                });
            }
        }

        /// <summary>
        /// Analyzes two schema models, identifies all differences, and generates a comprehensive
        /// migration script with detailed reporting of all changes.
        /// </summary>
        /// <param name="sourceSchema">The original database schema.</param>
        /// <param name="targetSchema">The target database schema to migrate to.</param>
        /// <returns>A detailed report of all changes and a complete SQL migration script.</returns>
        public static async Task<GetReportResponse> GenerateUpdateScripts(SchemaDesignerModel sourceSchema, SchemaDesignerModel targetSchema, DacSchemaDesigner sd)
        {
            // Validate inputs
            if (sourceSchema == null) throw new ArgumentNullException(nameof(sourceSchema));
            if (targetSchema == null) throw new ArgumentNullException(nameof(targetSchema));

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();
            var migrationScript = new StringBuilder();

            // Process the schema changes in a specific order to maintain dependencies:
            // 1. Process table structure changes (create, rename, alter, drop)
            // 2. Add new constraints and foreign keys
            // 3. Drop constraints and foreign keys that might block other operations

            // Step 2: Process table additions and modifications
            ProcessNewTables(sourceSchema, targetSchema, changeReport, migrationScript, sd);
            ProcessModifiedTables(sourceSchema, targetSchema, changeReport, migrationScript);

            // Step 3: Process foreign key changes for all tables
            ProcessForeignKeyChanges(sourceSchema, targetSchema, changeReport, migrationScript);

            // Step 1: First identify tables being dropped and handle their foreign keys
            ProcessDroppedTables(sourceSchema, targetSchema, changeReport, migrationScript, sd);

            // Convert the report dictionary to a list
            var reportObjectsList = changeReport.Values.ToList();

            return await Task.Run(() =>
            {
                string script = sd.GenerateScript();
                return new GetReportResponse()
                {
                    Reports = reportObjectsList,
                    UpdateScript = script
                };
            });

        }

        /// <summary>
        /// Identifies and processes tables that have been dropped between schemas.
        /// Handles dropping their foreign keys before dropping the tables themselves.
        /// </summary>
        internal static void ProcessDroppedTables(
            SchemaDesignerModel sourceSchema,
            SchemaDesignerModel targetSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            StringBuilder migrationScript,
            DacSchemaDesigner sd)
        {
            foreach (var sourceTable in sourceSchema.Tables)
            {
                var targetTable = targetSchema.Tables.FirstOrDefault(t => t.Id == sourceTable.Id);

                if (targetTable == null)
                {
                    // Table has been dropped - first drop all foreign keys
                    if (sourceTable.ForeignKeys != null && sourceTable.ForeignKeys.Count > 0)
                    {
                        foreach (var foreignKey in sourceTable.ForeignKeys)
                        {
                            string dropForeignKeyScript = $"ALTER TABLE [{sourceTable.Schema}].[{sourceTable.Name}] DROP CONSTRAINT [{foreignKey.Name}];\n";

                            TrackTableChange(
                                changeReport,
                                sourceTable,
                                SchemaDesignerReportTableState.DROPPED,
                                dropForeignKeyScript,
                                $"Dropping foreign key '{foreignKey.Name}' that referenced table '{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}'"
                            );

                            migrationScript.AppendLine(dropForeignKeyScript);
                        }
                    }

                    sd.TablesMarkedForDrop.Add([sourceTable.Schema, sourceTable.Name]);

                    // Now drop the table itself
                    string dropTableScript = $"DROP TABLE [{sourceTable.Schema}].[{sourceTable.Name}];\n";

                    TrackTableChange(
                        changeReport,
                        sourceTable,
                        SchemaDesignerReportTableState.DROPPED,
                        dropTableScript,
                        $"Dropping table '{sourceTable.Schema}.{sourceTable.Name}'"
                    );

                    migrationScript.AppendLine(dropTableScript);
                }
            }
        }

        /// <summary>
        /// Identifies and processes tables that have been newly added in the target schema.
        /// </summary>
        private static void ProcessNewTables(
            SchemaDesignerModel sourceSchema,
            SchemaDesignerModel targetSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            StringBuilder migrationScript,
            DacSchemaDesigner sd)
        {
            foreach (var targetTable in targetSchema.Tables)
            {
                var sourceTable = sourceSchema.Tables.FirstOrDefault(t => t.Id == targetTable.Id);

                if (sourceTable == null)
                {
                    // This is a new table - create it with all columns and constraints
                    string creationScript = SchemaCreationScriptGenerator.GenerateTableDefinition(targetTable);

                    var newTable = sd.CreateTable(targetTable.Schema, targetTable.Name);


                    for (var i = 0; i < targetTable.Columns.Count; i++)
                    {
                        var column = targetTable.Columns[i];
                        TableColumnViewModel sdColumn;
                        if (i == 0)
                 {
                            newTable.TableViewModel.Columns.Clear();
                        }

                        newTable.TableViewModel.Columns.AddNew();
                        sdColumn = newTable.TableViewModel.Columns.Items[i];

                        sdColumn.Name = column.Name;
                        sdColumn.DataType = column.DataType;
                        if (sdColumn.CanEditLength)
                        {
                            sdColumn.Length = column.MaxLength.ToString();
                        }
                        if (sdColumn.CanEditPrecision)
                        {
                            sdColumn.Precision = column.Precision;

                        }
                        if (sdColumn.CanEditScale)
                        {
                            sdColumn.Scale = column.Scale;

                        }
                        if (sdColumn.CanEditIsNullable)
                        {
                            sdColumn.IsNullable = column.IsNullable;

                        }
                        if (sdColumn.CanEditIsIdentity && column.IsIdentity)
                        {
                            sdColumn.IsIdentity = column.IsIdentity;
                            sdColumn.IdentitySeed = column.IdentitySeed;
                            sdColumn.IdentityIncrement = column.IdentityIncrement;
                        }

                        sdColumn.IsPrimaryKey = column.IsPrimaryKey;

                        if (sdColumn.CanEditDefaultValue)
                        {
                            sdColumn.DefaultValue = column.DefaultValue;

                        }
                    }

                    for (var i = 0; i < targetTable.ForeignKeys.Count; i++)
                    {
                        var foreignKey = targetTable.ForeignKeys[i];
                        ForeignKeyViewModel sdForeignKey;
                        if (i == 0)
                        {
                            sdForeignKey = newTable.TableViewModel.ForeignKeys.Items[0];
                        }
                        else
                        {
                            newTable.TableViewModel.ForeignKeys.AddNew();
                            sdForeignKey = newTable.TableViewModel.ForeignKeys.Items[i];
                        }
                        sdForeignKey.Name = foreignKey.Name;
                        sdForeignKey.ForeignTable = $"[{foreignKey.ReferencedSchemaName}].[{foreignKey.ReferencedTableName}]";
                        sdForeignKey.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(foreignKey.OnDeleteAction);
                        sdForeignKey.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(foreignKey.OnUpdateAction);
                        sdForeignKey.Columns.Clear();
                        sdForeignKey.Columns.AddRange(foreignKey.Columns);
                        sdForeignKey.ForeignColumns.Clear();
                        sdForeignKey.ForeignColumns.AddRange(foreignKey.ReferencedColumns);
                    }

                    TrackTableChange(
                        changeReport,
                        targetTable,
                        SchemaDesignerReportTableState.CREATED,
                        creationScript,
                        $"Creating new table '{targetTable.Schema}.{targetTable.Name}' with {targetTable.Columns.Count} column(s)"
                    );

                    migrationScript.AppendLine(creationScript);

                    // Note: Foreign keys for new tables will be handled in the dedicated foreign key processing step
                }
            }
        }

        /// <summary>
        /// Identifies and processes tables that exist in both schemas but have modifications.
        /// Handles renames, schema transfers, and column changes.
        /// </summary>
        private static void ProcessModifiedTables(
            SchemaDesignerModel sourceSchema,
            SchemaDesignerModel targetSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            StringBuilder migrationScript)
        {
            foreach (var sourceTable in sourceSchema.Tables)
            {
                var targetTable = targetSchema.Tables.FirstOrDefault(t => t.Id == sourceTable.Id);

                if (targetTable != null && !SchemaDesignerUtils.DeepCompareTable(sourceTable, targetTable))
                {
                    var tableUpdateScript = new StringBuilder();
                    bool hasModifications = false;

                    // Check for table name change
                    if (sourceTable.Name != targetTable.Name)
                    {
                        string renameScript = $"EXEC sp_rename '{sourceTable.Schema}.{sourceTable.Name}', '{targetTable.Name}';\n";
                        tableUpdateScript.AppendLine(renameScript);

                        TrackTableChange(
                            changeReport,
                            targetTable,
                            SchemaDesignerReportTableState.UPDATED,
                            null,
                            $"Renaming table from '{sourceTable.Name}' to '{targetTable.Name}'"
                        );

                        hasModifications = true;
                    }

                    // Check for schema change
                    if (sourceTable.Schema != targetTable.Schema)
                    {
                        string schemaChangeScript = $"ALTER SCHEMA [{targetTable.Schema}] TRANSFER [{sourceTable.Schema}].[{targetTable.Name}];\n";
                        tableUpdateScript.AppendLine(schemaChangeScript);

                        TrackTableChange(
                            changeReport,
                            targetTable,
                            SchemaDesignerReportTableState.UPDATED,
                            null,
                            $"Moving table from schema '{sourceTable.Schema}' to schema '{targetTable.Schema}'"
                        );

                        hasModifications = true;
                    }

                    // Process column changes
                    string columnUpdatesScript = GenerateColumnModificationScripts(sourceTable, targetTable, changeReport);
                    if (!string.IsNullOrEmpty(columnUpdatesScript))
                    {
                        tableUpdateScript.AppendLine(columnUpdatesScript);
                        hasModifications = true;
                    }

                    // If there were any modifications, add them to the migration script
                    if (hasModifications)
                    {
                        TrackTableChange(
                            changeReport,
                            targetTable,
                            SchemaDesignerReportTableState.UPDATED,
                            tableUpdateScript.ToString(),
                            $"Updating table structure for '{targetTable.Schema}.{targetTable.Name}'"
                        );

                        migrationScript.AppendLine(tableUpdateScript.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Processes all foreign key changes across schemas - adding new, modifying existing, and dropping removed foreign keys.
        /// </summary>
        private static void ProcessForeignKeyChanges(
            SchemaDesignerModel sourceSchema,
            SchemaDesignerModel targetSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            StringBuilder migrationScript)
        {
            // First handle foreign keys for new tables
            foreach (var targetTable in targetSchema.Tables)
            {
                var sourceTable = sourceSchema.Tables.FirstOrDefault(t => t.Id == targetTable.Id);

                if (sourceTable == null)
                {
                    // This is a new table - add all its foreign keys
                    if (targetTable.ForeignKeys != null)
                    {
                        foreach (var foreignKey in targetTable.ForeignKeys)
                        {
                            string fkScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(targetTable, foreignKey);

                            TrackTableChange(
                                changeReport,
                                targetTable,
                                SchemaDesignerReportTableState.CREATED,
                                fkScript,
                                $"Adding foreign key '{foreignKey.Name}' to reference table '{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}'"
                            );

                            migrationScript.AppendLine(fkScript);
                        }
                    }
                }
                else
                {
                    // This is an existing table - find foreign key changes
                    if (targetTable.ForeignKeys != null)
                    {
                        foreach (var targetForeignKey in targetTable.ForeignKeys)
                        {
                            var sourceForeignKey = sourceTable.ForeignKeys?.FirstOrDefault(fk => fk.Name == targetForeignKey.Name);

                            if (sourceForeignKey == null)
                            {
                                // This is a new foreign key
                                string fkScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(targetTable, targetForeignKey);

                                TrackTableChange(
                                    changeReport,
                                    targetTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    fkScript,
                                    $"Adding new foreign key '{targetForeignKey.Name}' to reference table '{targetForeignKey.ReferencedSchemaName}.{targetForeignKey.ReferencedTableName}'"
                                );

                                migrationScript.AppendLine(fkScript);
                            }
                            else if (!SchemaDesignerUtils.DeepCompareForeignKey(sourceForeignKey, targetForeignKey))
                            {
                                // Foreign key exists but has changed - drop and recreate
                                string dropFkScript = $"ALTER TABLE [{targetTable.Schema}].[{targetTable.Name}] DROP CONSTRAINT [{sourceForeignKey.Name}];\n";
                                string addFkScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(targetTable, targetForeignKey);

                                TrackTableChange(
                                    changeReport,
                                    targetTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    dropFkScript + addFkScript,
                                    $"Modifying foreign key '{targetForeignKey.Name}' ({GetForeignKeyChangeDescription(sourceForeignKey, targetForeignKey)})"
                                );

                                migrationScript.AppendLine(dropFkScript);
                                migrationScript.AppendLine(addFkScript);
                            }
                        }
                    }

                    // Find and drop foreign keys that have been removed
                    if (sourceTable.ForeignKeys != null)
                    {
                        foreach (var sourceForeignKey in sourceTable.ForeignKeys)
                        {
                            bool foreignKeyExists = targetTable.ForeignKeys?.Any(fk => fk.Name == sourceForeignKey.Name) ?? false;

                            if (!foreignKeyExists)
                            {
                                string dropFkScript = $"ALTER TABLE [{sourceTable.Schema}].[{sourceTable.Name}] DROP CONSTRAINT [{sourceForeignKey.Name}];\n";

                                TrackTableChange(
                                    changeReport,
                                    targetTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    dropFkScript,
                                    $"Removing foreign key '{sourceForeignKey.Name}' that referenced table '{sourceForeignKey.ReferencedSchemaName}.{sourceForeignKey.ReferencedTableName}'"
                                );

                                migrationScript.AppendLine(dropFkScript);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Generates a descriptive message about what has changed in a foreign key.
        /// </summary>
        private static string GetForeignKeyChangeDescription(SchemaDesignerForeignKey source, SchemaDesignerForeignKey target)
        {
            var changes = new List<string>();

            if (source.ReferencedTableName != target.ReferencedTableName ||
                source.ReferencedSchemaName != target.ReferencedSchemaName)
            {
                changes.Add($"reference changed from '{source.ReferencedSchemaName}.{source.ReferencedTableName}' to '{target.ReferencedSchemaName}.{target.ReferencedTableName}'");
            }

            // Compare columns
            if (!source.Columns.SequenceEqual(target.Columns))
            {
                changes.Add($"columns changed from ({string.Join(", ", source.Columns)}) to ({string.Join(", ", target.Columns)})");
            }

            // Compare referenced columns
            if (!source.ReferencedColumns.SequenceEqual(target.ReferencedColumns))
            {
                changes.Add($"referenced columns changed from ({string.Join(", ", source.ReferencedColumns)}) to ({string.Join(", ", target.ReferencedColumns)})");
            }

            // Compare actions
            if (source.OnDeleteAction != target.OnDeleteAction)
            {
                changes.Add($"ON DELETE action changed from {source.OnDeleteAction} to {target.OnDeleteAction}");
            }

            if (source.OnUpdateAction != target.OnUpdateAction)
            {
                changes.Add($"ON UPDATE action changed from {source.OnUpdateAction} to {target.OnUpdateAction}");
            }

            return string.Join(", ", changes);
        }

        /// <summary>
        /// Generates SQL scripts for all column modifications in a table.
        /// </summary>
        /// <param name="sourceTable">The original table definition.</param>
        /// <param name="targetTable">The target table definition.</param>
        /// <param name="changeReport">Report dictionary to update with changes.</param>
        /// <returns>A complete SQL script for all column modifications.</returns>
        private static string GenerateColumnModificationScripts(
            SchemaDesignerTable sourceTable,
            SchemaDesignerTable targetTable,
            Dictionary<string, SchemaDesignerReportObject> changeReport)
        {
            var scriptBuilder = new StringBuilder();

            // Process columns to drop first
            foreach (var sourceColumn in sourceTable.Columns)
            {
                var targetColumn = targetTable.Columns.FirstOrDefault(c => c.Id == sourceColumn.Id);

                if (targetColumn == null)
                {
                    // Column has been dropped
                    string dropColumnScript = $"ALTER TABLE [{sourceTable.Schema}].[{sourceTable.Name}] DROP COLUMN [{sourceColumn.Name}];\n";
                    scriptBuilder.AppendLine(dropColumnScript);

                    TrackTableChange(
                        changeReport,
                        targetTable,
                        SchemaDesignerReportTableState.UPDATED,
                        null,
                        $"Dropping column '{sourceColumn.Name}' of type '{sourceColumn.DataType}'"
                    );
                }
            }

            // Process column additions and modifications
            foreach (var targetColumn in targetTable.Columns)
            {
                var sourceColumn = sourceTable.Columns.FirstOrDefault(c => c.Id == targetColumn.Id);

                if (sourceColumn == null)
                {
                    // New column
                    string addColumnScript = $"ALTER TABLE [{targetTable.Schema}].[{targetTable.Name}] ADD {SchemaCreationScriptGenerator.GenerateColumnDefinition(targetColumn)};\n";
                    scriptBuilder.AppendLine(addColumnScript);

                    TrackTableChange(
                        changeReport,
                        targetTable,
                        SchemaDesignerReportTableState.UPDATED,
                        null,
                        $"Adding new column '{targetColumn.Name}' of type '{targetColumn.DataType}'"
                    );
                }
                else if (!SchemaDesignerUtils.DeepCompareColumn(sourceColumn, targetColumn))
                {
                    // Modified column
                    string alterColumnScript = $"ALTER TABLE [{targetTable.Schema}].[{targetTable.Name}] ALTER COLUMN {SchemaCreationScriptGenerator.GenerateColumnDefinition(targetColumn)};\n";
                    scriptBuilder.AppendLine(alterColumnScript);

                    TrackTableChange(
                        changeReport,
                        targetTable,
                        SchemaDesignerReportTableState.UPDATED,
                        null,
                        $"Modifying column '{targetColumn.Name}' ({GetColumnChangeDescription(sourceColumn, targetColumn)})"
                    );
                }
            }

            return scriptBuilder.ToString();
        }

        /// <summary>
        /// Generates a descriptive message about what has changed in a column.
        /// </summary>
        internal static string GetColumnChangeDescription(SchemaDesignerColumn source, SchemaDesignerColumn target)
        {
            var changes = new List<string>();
            if (source.DataType != target.DataType)
            {
                changes.Add($"type changed from '{source.DataType}' to '{target.DataType}'");
            }

            if (source.MaxLength != target.MaxLength)
            {
                string? sourceLength = source.MaxLength.HasValue ? source.MaxLength.ToString() : "NULL";
                string? targetLength = target.MaxLength.HasValue ? target.MaxLength.ToString() : "NULL";
                changes.Add($"length changed from {sourceLength} to {targetLength}");
            }

            if (source.Precision != target.Precision || source.Scale != target.Scale)
            {
                string sourcePrecision = source.Precision.HasValue ? $"{source.Precision}" : "NULL";
                string sourceScale = source.Scale.HasValue ? $",{source.Scale}" : "";
                string targetPrecision = target.Precision.HasValue ? $"{target.Precision}" : "NULL";
                string targetScale = target.Scale.HasValue ? $",{target.Scale}" : "";

                changes.Add($"precision/scale changed from ({sourcePrecision}{sourceScale}) to ({targetPrecision}{targetScale})");
            }

            if (source.IsNullable != target.IsNullable)
            {
                changes.Add($"nullability changed from {(source.IsNullable ? "NULL" : "NOT NULL")} to {(target.IsNullable ? "NULL" : "NOT NULL")}");
            }

            if (source.IsPrimaryKey != target.IsPrimaryKey)
            {
                if (target.IsPrimaryKey)
                    changes.Add("added to primary key");
                else
                    changes.Add("removed from primary key");
            }

            if (source.IsUnique != target.IsUnique)
            {
                if (target.IsUnique)
                    changes.Add("added unique constraint");
                else
                    changes.Add("removed unique constraint");
            }

            if (source.IsIdentity != target.IsIdentity)
            {
                if (target.IsIdentity)
                    changes.Add("added identity property");
                else
                    changes.Add("removed identity property");
            }
            else if (source.IsIdentity && target.IsIdentity &&
                    (source.IdentitySeed != target.IdentitySeed || source.IdentityIncrement != target.IdentityIncrement))
            {
                changes.Add($"identity values changed from ({source.IdentitySeed},{source.IdentityIncrement}) to ({target.IdentitySeed},{target.IdentityIncrement})");
            }

            if (source.Collation != target.Collation)
            {
                string sourceCollation = string.IsNullOrEmpty(source.Collation) ? "NULL" : source.Collation;
                string targetCollation = string.IsNullOrEmpty(target.Collation) ? "NULL" : target.Collation;
                changes.Add($"collation changed from {sourceCollation} to {targetCollation}");
            }

            if (source.DefaultValue != target.DefaultValue)
            {
                changes.Add($"default value changed from {source.DefaultValue} to ${target.DefaultValue}");
            }

            return string.Join(", ", changes);
        }
    }
}