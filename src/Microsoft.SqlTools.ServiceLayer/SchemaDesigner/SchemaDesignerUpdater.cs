//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Sql.DesignServices.TableDesigner;
using DacSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;
using DacTableDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.TableDesigner;


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
        private static void TrackTableChange(
            Dictionary<string, SchemaDesignerChangeReport> reportMap,
            SchemaDesignerTable table,
            SchemaDesignerReportTableState tableState,
            string changeDescription)
        {
            string tableId = table.Id.ToString();

            if (reportMap.TryGetValue(tableId, out SchemaDesignerChangeReport? existingReport))
            {
                if (tableState > existingReport.TableState)
                {
                    existingReport.TableState = tableState;
                }
                existingReport.ActionsPerformed.Add(changeDescription);
            }
            else
            {
                reportMap.Add(tableId, new SchemaDesignerChangeReport
                {
                    TableId = table.Id,
                    TableName = $"{table.Schema}.{table.Name}",
                    TableState = tableState,
                    ActionsPerformed = new List<string> { changeDescription }
                });
            }
        }

        /// <summary>
        /// Analyzes two schema models, identifies all differences, and generates a comprehensive
        /// migration script with detailed reporting of all changes.
        /// </summary>
        /// <param name="initialSchema">The original database schema.</param>
        /// <param name="updatedSchema">The target database schema to migrate to.</param>
        /// <returns>A detailed report of all changes and a complete SQL migration script.</returns>
        public static async Task<GetReportResponse> GenerateUpdateScripts(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            DacSchemaDesigner schemaDesigner)
        {
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            var changeReport = new Dictionary<string, SchemaDesignerChangeReport>();

            /**
            * 1. Process new tables and modified tables first to ensure that all new structures are in place before adding foreign keys.
            * 2. Process foreign key changes to ensure that all relationships are established correctly.
            * 3. Finally, process dropped tables to remove any deleted tables and their associated foreign keys.
            */
            ProcessNewTables(initialSchema, updatedSchema, changeReport, schemaDesigner);
            ProcessModifiedTables(initialSchema, updatedSchema, changeReport, schemaDesigner);
            ProcessForeignKeyChanges(initialSchema, updatedSchema, changeReport, schemaDesigner);
            ProcessDroppedTables(initialSchema, updatedSchema, changeReport, schemaDesigner);

            return await Task.Run(() =>
            {
                return new GetReportResponse()
                {
                    Reports = changeReport.Values.ToList(),
                    UpdateScript = schemaDesigner.GenerateScript(),
                };
            });

        }

        /// <summary>
        /// Process table that have been dropped.
        /// </summary>
        internal static void ProcessDroppedTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerChangeReport> report,
            DacSchemaDesigner schemaDesigner)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            foreach (var sourceTable in initialSchema.Tables)
            {
                if (!updatedSchema.Tables.Any(t => t.Id == sourceTable.Id))
                {

                    // Drop all foreign keys first
                    if (sourceTable.ForeignKeys?.Count > 0)
                    {
                        foreach (var foreignKey in sourceTable.ForeignKeys)
                        {
                            TrackTableChange(
                                report,
                                sourceTable,
                                SchemaDesignerReportTableState.DROPPED,
                                $"Dropping foreign key '{foreignKey.Name}' that referenced table '{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}'"
                            );
                        }
                    }

                    // Mark for drop in designer
                    schemaDesigner.TablesMarkedForDrop.Add([sourceTable.Schema, sourceTable.Name]);

                    // Log the change
                    TrackTableChange(
                        report,
                        sourceTable,
                        SchemaDesignerReportTableState.DROPPED,
                        $"Dropping table '{sourceTable.Schema}.{sourceTable.Name}'"
                    );
                }
            }
        }

        /// <summary>
        /// Process tables that are newly added
        /// </summary>
        private static void ProcessNewTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerChangeReport> report,
            DacSchemaDesigner schemaDesigner)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            foreach (var targetTable in updatedSchema.Tables)
            {
                if (!initialSchema.Tables.Any(t => t.Id == targetTable.Id))
                {
                    // Create new table in designer
                    var tableDesigner = schemaDesigner.CreateTable(targetTable.Schema, targetTable.Name);
                    tableDesigner.TableViewModel.Schema = targetTable.Schema;

                    if (targetTable.Columns != null && targetTable.Columns.Count > 0)
                    {
                        // Add columns
                        AddColumnsToTableDesigner(tableDesigner, targetTable.Columns);
                    }

                    // Log the change
                    TrackTableChange(
                        report,
                        targetTable,
                        SchemaDesignerReportTableState.CREATED,
                        $"Creating new table '{targetTable.Schema}.{targetTable.Name}' with {targetTable.Columns?.Count ?? 0} column(s)"
                    );
                }
            }
        }

        /// <summary>
        /// Add multiple columns to a table designer
        /// </summary>
        private static void AddColumnsToTableDesigner(DacTableDesigner tableDesigner, List<SchemaDesignerColumn> columns)
        {
            if (columns == null || columns.Count == 0) return;

            // Clear existing columns and add new ones
            tableDesigner.TableViewModel.Columns.Clear();

            foreach (var column in columns)
            {
                tableDesigner.TableViewModel.Columns.AddNew();
                var sdColumn = tableDesigner.TableViewModel.Columns.Items.Last();
                SetColumnProperties(sdColumn, column);
            }
        }

        /// <summary>
        /// Sets properties on a table column view model from a schema column
        /// </summary>
        private static void SetColumnProperties(TableColumnViewModel viewModel, SchemaDesignerColumn column)
        {
            viewModel.Name = column.Name;
            viewModel.DataType = column.DataType;

            if (viewModel.CanEditLength && column.MaxLength != null)
            {
                viewModel.Length = column.MaxLength;
            }

            if (viewModel.CanEditPrecision && column.Precision.HasValue)
            {
                viewModel.Precision = column.Precision;
            }

            if (viewModel.CanEditScale && column.Scale.HasValue)
            {
                viewModel.Scale = column.Scale;
            }

            if (viewModel.CanEditIsNullable)
            {
                viewModel.IsNullable = column.IsNullable;
            }

            if (viewModel.CanEditIsIdentity && column.IsIdentity)
            {
                viewModel.IsIdentity = column.IsIdentity;
                viewModel.IdentitySeed = column.IdentitySeed;
                viewModel.IdentityIncrement = column.IdentityIncrement;
            }

            viewModel.IsPrimaryKey = column.IsPrimaryKey;

            if (viewModel.CanEditDefaultValue)
            {
                viewModel.DefaultValue = column.DefaultValue;
            }
        }



        /// <summary>
        /// Identifies and processes tables that exist in both schemas but have modifications.
        /// Handles renames, schema transfers, and column changes.
        /// </summary>
        private static void ProcessModifiedTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerChangeReport> changeReport,
            DacSchemaDesigner sd)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            foreach (var initialTable in initialSchema.Tables)
            {
                var updatedTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == initialTable.Id);

                if (updatedTable != null && !SchemaDesignerUtils.DeepCompareTable(initialTable, updatedTable))
                {
                    var tableDesigner = sd.GetTableDesigner(initialTable.Schema, initialTable.Name);
                    bool hasChanges = false;

                    // Update table name if changed
                    if (initialTable.Name != updatedTable.Name)
                    {
                        tableDesigner.TableViewModel.Name = updatedTable.Name;
                        TrackTableChange(
                            changeReport,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Renaming table from '{initialTable.Name}' to '{updatedTable.Name}'"
                        );
                        hasChanges = true;
                    }

                    // Update table name if changed
                    if (initialTable.Schema != updatedTable.Schema)
                    {
                        tableDesigner.TableViewModel.Schema = updatedTable.Schema;
                        TrackTableChange(
                            changeReport,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Moving table from schema '{initialTable.Schema}' to schema '{updatedTable.Schema}'"
                        );
                        hasChanges = true;
                    }

                    // Process column changes
                    if (UpdateTableColumns(initialTable, updatedTable, changeReport, tableDesigner))
                    {
                        hasChanges = true;
                    }

                    // If there were any modifications, add them to the migration script
                    if (hasChanges)
                    {
                        TrackTableChange(
                            changeReport,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Updating table structure for '{updatedTable.Schema}.{updatedTable.Name}'"
                        );

                    }
                }
            }
        }

        /// <summary>
        /// Update columns in an existing table
        /// </summary>
        private static bool UpdateTableColumns(
            SchemaDesignerTable initialTable,
            SchemaDesignerTable updatedTable,
            Dictionary<string, SchemaDesignerChangeReport> report,
            DacTableDesigner tableDesigner)
        {
            if (initialTable.Columns == null || updatedTable.Columns == null) return false;

            bool hasChanges = false;
            int index = 0;

            // First drop columns that don't exist in the target
            foreach (var sourceColumn in initialTable.Columns.ToList())
            {
                if (!updatedTable.Columns.Any(c => c.Id == sourceColumn.Id))
                {
                    tableDesigner.TableViewModel.Columns.RemoveAt(index);

                    TrackTableChange(
                        report,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Dropping column '{sourceColumn.Name}' of type '{sourceColumn.DataType}'"
                    );
                    hasChanges = true;
                }
                else
                {
                    index++;
                }
            }

            // Then add/modify columns
            foreach (var targetColumn in updatedTable.Columns)
            {
                var sourceColumn = initialTable.Columns.FirstOrDefault(c => c.Id == targetColumn.Id);
                var existingCol = tableDesigner.TableViewModel.Columns.Items.FirstOrDefault(c => c.Name == targetColumn.Name);

                if (sourceColumn == null && existingCol == null)
                {
                    // Add new column
                    tableDesigner.TableViewModel.Columns.AddNew();
                    var newCol = tableDesigner.TableViewModel.Columns.Items.Last();
                    SetColumnProperties(newCol, targetColumn);

                    TrackTableChange(
                        report,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Adding new column '{targetColumn.Name}' of type '{targetColumn.DataType}'"
                    );

                    hasChanges = true;
                }
                else if (sourceColumn != null && !SchemaDesignerUtils.DeepCompareColumn(sourceColumn, targetColumn))
                {
                    // Modify existing column
                    var viewModel = tableDesigner.TableViewModel.Columns.Items.First(c => c.Name == sourceColumn.Name);

                    // Only update properties that have changed
                    UpdateColumnProperties(viewModel, sourceColumn, targetColumn);

                    TrackTableChange(
                        report,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Modifying column '{targetColumn.Name}' ({GetColumnChanges(sourceColumn, targetColumn)})"
                    );

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Updates only the properties that have changed between source and target columns
        /// </summary>
        private static void UpdateColumnProperties(
            TableColumnViewModel viewModel,
            SchemaDesignerColumn sourceColumn,
            SchemaDesignerColumn targetColumn)
        {
            // Only update name if it changed
            if (sourceColumn.Name != targetColumn.Name)
            {
                viewModel.Name = targetColumn.Name;
            }

            // Only update data type if it changed
            if (sourceColumn.DataType != targetColumn.DataType)
            {
                viewModel.DataType = targetColumn.DataType;
            }

            // Only update other properties if they changed
            if (viewModel.CanEditLength && sourceColumn.MaxLength != targetColumn.MaxLength)
            {
                viewModel.Length = targetColumn.MaxLength;
            }

            if (viewModel.CanEditPrecision && sourceColumn.Precision != targetColumn.Precision && targetColumn.Precision.HasValue)
            {
                viewModel.Precision = targetColumn.Precision;
            }

            if (viewModel.CanEditScale && sourceColumn.Scale != targetColumn.Scale && targetColumn.Scale.HasValue)
            {
                viewModel.Scale = targetColumn.Scale;
            }

            if (viewModel.CanEditIsNullable && sourceColumn.IsNullable != targetColumn.IsNullable)
            {
                viewModel.IsNullable = targetColumn.IsNullable;
            }

            if (viewModel.CanEditIsIdentity && sourceColumn.IsIdentity != targetColumn.IsIdentity)
            {
                viewModel.IsIdentity = targetColumn.IsIdentity;

                if (targetColumn.IsIdentity)
                {
                    viewModel.IdentitySeed = targetColumn.IdentitySeed;
                    viewModel.IdentityIncrement = targetColumn.IdentityIncrement;
                }
            }
            else if (viewModel.CanEditIsIdentity && sourceColumn.IsIdentity && targetColumn.IsIdentity &&
                     (sourceColumn.IdentitySeed != targetColumn.IdentitySeed ||
                      sourceColumn.IdentityIncrement != targetColumn.IdentityIncrement))
            {
                viewModel.IdentitySeed = targetColumn.IdentitySeed;
                viewModel.IdentityIncrement = targetColumn.IdentityIncrement;
            }

            if (sourceColumn.IsPrimaryKey != targetColumn.IsPrimaryKey)
            {
                viewModel.IsPrimaryKey = targetColumn.IsPrimaryKey;
            }

            if (viewModel.CanEditDefaultValue && sourceColumn.DefaultValue != targetColumn.DefaultValue)
            {
                viewModel.DefaultValue = targetColumn.DefaultValue;
            }
        }

        /// <summary>
        /// Creates a description of column changes
        /// </summary>
        private static string GetColumnChanges(SchemaDesignerColumn source, SchemaDesignerColumn target)
        {
            var changes = new List<string>();

            if (source.DataType != target.DataType)
            {
                changes.Add($"type changed from '{source.DataType}' to '{target.DataType}'");
            }

            if (source.MaxLength != target.MaxLength)
            {
                string sourceLength = source.MaxLength?.ToString() ?? "NULL";
                string targetLength = target.MaxLength?.ToString() ?? "NULL";
                changes.Add($"length changed from {sourceLength} to {targetLength}");
            }

            if (source.Precision != target.Precision || source.Scale != target.Scale)
            {
                string sourcePrecision = source.Precision?.ToString() ?? "NULL";
                string sourceScale = source.Scale.HasValue ? $",{source.Scale}" : "";
                string targetPrecision = target.Precision?.ToString() ?? "NULL";
                string targetScale = target.Scale.HasValue ? $",{target.Scale}" : "";

                changes.Add($"precision/scale changed from ({sourcePrecision}{sourceScale}) to ({targetPrecision}{targetScale})");
            }

            if (source.IsNullable != target.IsNullable)
            {
                changes.Add($"nullability changed from {(source.IsNullable ? "NULL" : "NOT NULL")} to {(target.IsNullable ? "NULL" : "NOT NULL")}");
            }

            if (source.IsPrimaryKey != target.IsPrimaryKey)
            {
                changes.Add(target.IsPrimaryKey ? "added to primary key" : "removed from primary key");
            }

            if (source.IsIdentity != target.IsIdentity)
            {
                changes.Add(target.IsIdentity ? "added identity property" : "removed identity property");
            }
            else if (source.IsIdentity && target.IsIdentity &&
                    (source.IdentitySeed != target.IdentitySeed || source.IdentityIncrement != target.IdentityIncrement))
            {
                changes.Add($"identity values changed from ({source.IdentitySeed},{source.IdentityIncrement}) to ({target.IdentitySeed},{target.IdentityIncrement})");
            }

            if (source.DefaultValue != target.DefaultValue)
            {
                string sourceDefault = string.IsNullOrEmpty(source.DefaultValue) ? "NULL" : source.DefaultValue;
                string targetDefault = string.IsNullOrEmpty(target.DefaultValue) ? "NULL" : target.DefaultValue;
                changes.Add($"default value changed from {sourceDefault} to {targetDefault}");
            }

            return string.Join(", ", changes);
        }

        /// <summary>
        /// Processes all foreign key changes across schemas - adding new, modifying existing, and dropping removed foreign keys.
        /// </summary>
        private static void ProcessForeignKeyChanges(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerChangeReport> changeReport,
            DacSchemaDesigner schemaDesigner)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            // Handle foreign keys in existing and new tables
            foreach (var targetTable in updatedSchema.Tables)
            {
                var sourceTable = initialSchema.Tables.FirstOrDefault(t => t.Id == targetTable.Id);

                if (sourceTable == null)
                {
                    // For new tables, add all foreign keys
                    if (targetTable.ForeignKeys?.Count > 0)
                    {
                        var tableDesigner = schemaDesigner.GetTableDesigner(targetTable.Schema, targetTable.Name);
                        AddForeignKeysToTableDesigner(tableDesigner, targetTable, targetTable.ForeignKeys, changeReport);
                    }
                }
                else
                {
                    // For existing tables, process foreign key changes
                    UpdateTableForeignKeys(sourceTable, targetTable, changeReport, schemaDesigner);
                }
            }
        }

        /// <summary>
        /// Add multiple foreign keys to a table designer
        /// </summary>
        private static void AddForeignKeysToTableDesigner(
            DacTableDesigner tableDesigner,
            SchemaDesignerTable table,
            List<SchemaDesignerForeignKey> foreignKeys,
            Dictionary<string, SchemaDesignerChangeReport> report)
        {
            foreach (var fk in foreignKeys)
            {
                tableDesigner.TableViewModel.ForeignKeys.AddNew();
                var sdForeignKey = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                SetForeignKeyProperties(sdForeignKey, fk);

                TrackTableChange(
                    report,
                    table,
                    SchemaDesignerReportTableState.CREATED,
                    $"Adding foreign key '{fk.Name}' to reference table '{fk.ReferencedSchemaName}.{fk.ReferencedTableName}'"
                );
            }
        }

        /// <summary>
        /// Sets properties on a foreign key view model from a schema foreign key
        /// </summary>
        private static void SetForeignKeyProperties(ForeignKeyViewModel viewModel, SchemaDesignerForeignKey fk)
        {
            viewModel.Name = fk.Name;
            viewModel.ForeignTable = $"{fk.ReferencedSchemaName}.{fk.ReferencedTableName}";
            viewModel.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(fk.OnDeleteAction);
            viewModel.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(fk.OnUpdateAction);

            viewModel.Columns.Clear();

            if (fk.Columns != null && fk.ReferencedColumns != null)
            {
                for (int i = 0; i < fk.Columns.Count; i++)
                {
                    viewModel.AddNewColumnMapping();
                    viewModel.UpdateColumn(i, fk.Columns[i]);
                    viewModel.UpdateForeignColumn(i, fk.ReferencedColumns[i]);
                }
            }
        }

        /// <summary>
        /// Update foreign keys in an existing table
        /// </summary>
        private static void UpdateTableForeignKeys(
            SchemaDesignerTable initialTable,
            SchemaDesignerTable updatedTable,
            Dictionary<string, SchemaDesignerChangeReport> report,
            DacSchemaDesigner designer)
        {
            if (initialTable.ForeignKeys == null && updatedTable.ForeignKeys == null) return;

            var tableDesigner = designer.GetTableDesigner(updatedTable.Schema, updatedTable.Name);

            // Remove foreign keys that don't exist in the target
            if (initialTable.ForeignKeys != null)
            {
                for (int i = initialTable.ForeignKeys.Count - 1; i >= 0; i--)
                {
                    var sourceFk = initialTable.ForeignKeys[i];
                    if (updatedTable.ForeignKeys == null || !updatedTable.ForeignKeys.Any(fk => fk.Id == sourceFk.Id))
                    {
                        tableDesigner.TableViewModel.ForeignKeys.RemoveAt(i);

                        TrackTableChange(
                            report,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Removing foreign key '{sourceFk.Name}' that referenced table '{sourceFk.ReferencedSchemaName}.{sourceFk.ReferencedTableName}'"
                        );
                    }
                }
            }

            // Add/modify foreign keys
            if (updatedTable.ForeignKeys != null)
            {
                foreach (var targetFk in updatedTable.ForeignKeys)
                {
                    var sourceFk = initialTable.ForeignKeys?.FirstOrDefault(fk => fk.Id == targetFk.Id);

                    if (sourceFk == null)
                    {
                        // Add new foreign key
                        tableDesigner.TableViewModel.ForeignKeys.AddNew();
                        var newFk = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                        SetForeignKeyProperties(newFk, targetFk);

                        TrackTableChange(
                            report,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Adding new foreign key '{targetFk.Name}' to reference table '{targetFk.ReferencedSchemaName}.{targetFk.ReferencedTableName}'"
                        );
                    }
                    else if (!SchemaDesignerUtils.DeepCompareForeignKey(sourceFk, targetFk))
                    {
                        // Update existing foreign key
                        int index = tableDesigner.TableViewModel.ForeignKeys.Items.ToList().FindIndex(fk => fk.Name == sourceFk.Name);
                        if (index >= 0)
                        {
                            var viewModel = tableDesigner.TableViewModel.ForeignKeys.Items[index];
                            SetForeignKeyProperties(viewModel, targetFk);

                            TrackTableChange(
                                report,
                                updatedTable,
                                SchemaDesignerReportTableState.UPDATED,
                                $"Modifying foreign key '{targetFk.Name}' ({GetForeignKeyChanges(sourceFk, targetFk)})"
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a description of foreign key changes
        /// </summary>
        private static string GetForeignKeyChanges(SchemaDesignerForeignKey source, SchemaDesignerForeignKey target)
        {
            var changes = new List<string>();

            if (source.ReferencedTableName != target.ReferencedTableName ||
                source.ReferencedSchemaName != target.ReferencedSchemaName)
            {
                changes.Add($"reference changed from '{source.ReferencedSchemaName}.{source.ReferencedTableName}' to '{target.ReferencedSchemaName}.{target.ReferencedTableName}'");
            }

            if (!SequenceEqual(source.Columns, target.Columns))
            {
                changes.Add($"columns changed from ({JoinStrings(source.Columns)}) to ({JoinStrings(target.Columns)})");
            }

            if (!SequenceEqual(source.ReferencedColumns, target.ReferencedColumns))
            {
                changes.Add($"referenced columns changed from ({JoinStrings(source.ReferencedColumns)}) to ({JoinStrings(target.ReferencedColumns)})");
            }

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

        private static bool SequenceEqual<T>(List<T> list1, List<T> list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            return list1.SequenceEqual(list2);
        }

        private static string JoinStrings(List<string> strings)
        {
            return strings == null ? "" : string.Join(", ", strings);
        }

    }
}