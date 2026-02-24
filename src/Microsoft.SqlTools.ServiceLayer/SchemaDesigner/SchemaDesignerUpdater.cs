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

            /**
            * 1. Process new tables and modified tables first to ensure that all new structures are in place before adding foreign keys.
            * 2. Process foreign key changes to ensure that all relationships are established correctly.
            * 3. Finally, process dropped tables to remove any deleted tables and their associated foreign keys.
            */
            ProcessNewTables(initialSchema, updatedSchema, schemaDesigner);
            ProcessModifiedTables(initialSchema, updatedSchema, schemaDesigner);
            ProcessForeignKeyChanges(initialSchema, updatedSchema, schemaDesigner);
            ProcessDroppedTables(initialSchema, updatedSchema, schemaDesigner);

            return await Task.Run(() =>
            {
                var hasSchemaChanged = schemaDesigner.TableDesigners.Count != 0;
                return new GetReportResponse()
                {
                    HasSchemaChanged = hasSchemaChanged,
                    DacReport = hasSchemaChanged ? schemaDesigner.GeneratePreviewReport(): null,
                };
            });

        }

        /// <summary>
        /// Process table that have been dropped.
        /// </summary>
        internal static void ProcessDroppedTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            DacSchemaDesigner schemaDesigner)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            foreach (var sourceTable in initialSchema.Tables)
            {
                if (!updatedSchema.Tables.Any(t => t.Id == sourceTable.Id))
                {
                    // Mark for drop in designer
                    schemaDesigner.TablesMarkedForDrop.Add([sourceTable.Schema, sourceTable.Name]);
                }
            }
        }

        /// <summary>
        /// Process tables that are newly added
        /// </summary>
        private static void ProcessNewTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
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

            if (column.IsComputed)
            {
                viewModel.IsComputed = column.IsComputed;
                viewModel.ComputedFormula = column.ComputedFormula;
                viewModel.IsComputedPersisted = column.ComputedPersisted;
                if (viewModel.CanEditIsComputedPersistedNullable)
                {
                    viewModel.IsComputedPersistedNullable = column.IsNullable;
                }
            }
            else
            {
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

            if (viewModel.CanEditIsNullable)
            {
                viewModel.IsNullable = column.IsNullable;
            }
        }



        /// <summary>
        /// Identifies and processes tables that exist in both schemas but have modifications.
        /// Handles renames, schema transfers, and column changes.
        /// </summary>
        private static void ProcessModifiedTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            DacSchemaDesigner sd)
        {
            if (initialSchema?.Tables == null || updatedSchema?.Tables == null) return;

            foreach (var initialTable in initialSchema.Tables)
            {
                var updatedTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == initialTable.Id);

                if (updatedTable != null && !SchemaDesignerUtils.DeepCompareTable(initialTable, updatedTable))
                {
                    var tableDesigner = sd.GetTableDesigner(initialTable.Schema, initialTable.Name);

                    // Update table name if changed
                    if (initialTable.Name != updatedTable.Name)
                    {
                        tableDesigner.TableViewModel.Name = updatedTable.Name;
                    }

                    // Update table name if changed
                    if (initialTable.Schema != updatedTable.Schema)
                    {
                        tableDesigner.TableViewModel.Schema = updatedTable.Schema;
                    }

                    // Process column changes
                    UpdateTableColumns(initialTable, updatedTable, tableDesigner);
                }
            }
        }

        /// <summary>
        /// Update columns in an existing table
        /// </summary>
        private static void UpdateTableColumns(
            SchemaDesignerTable initialTable,
            SchemaDesignerTable updatedTable,
            DacTableDesigner tableDesigner)
        {
            if (initialTable.Columns == null || updatedTable.Columns == null) return;

            var columnNameById = initialTable.Columns
                .GroupBy(column => column.Id)
                .ToDictionary(group => group.Key, group => group.First().Name);

            int index = 0;

            // First drop columns that don't exist in the target
            foreach (var sourceColumn in initialTable.Columns.ToList())
            {
                if (!updatedTable.Columns.Any(c => c.Id == sourceColumn.Id))
                {
                    tableDesigner.TableViewModel.Columns.RemoveAt(index);
                    columnNameById.Remove(sourceColumn.Id);
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
                    columnNameById[targetColumn.Id] = targetColumn.Name;
                }
                else if (sourceColumn != null && !SchemaDesignerUtils.DeepCompareColumn(sourceColumn, targetColumn))
                {
                    // Modify existing column
                    var viewModel = tableDesigner.TableViewModel.Columns.Items.First(c => c.Name == sourceColumn.Name);

                    // Only update properties that have changed
                    UpdateColumnProperties(viewModel, sourceColumn, targetColumn);
                    columnNameById[targetColumn.Id] = targetColumn.Name;
                }
                else if (sourceColumn != null)
                {
                    columnNameById[targetColumn.Id] = targetColumn.Name;
                }
            }

            // Reorder columns when IDs are in a different order.
            if (AreColumnOrdersDifferent(initialTable.Columns, updatedTable.Columns))
            {
                for (int targetIndex = 0; targetIndex < updatedTable.Columns.Count; targetIndex++)
                {
                    var targetColumn = updatedTable.Columns[targetIndex];
                    var targetColumnName = columnNameById.TryGetValue(targetColumn.Id, out var name)
                        ? name
                        : targetColumn.Name;

                    if (string.IsNullOrEmpty(targetColumnName))
                    {
                        continue;
                    }

                    var currentIndex = tableDesigner.TableViewModel.Columns.Items
                        .ToList()
                        .FindIndex(c => string.Equals(c.Name, targetColumnName, StringComparison.OrdinalIgnoreCase));

                    if (currentIndex >= 0 && currentIndex != targetIndex)
                    {
                        tableDesigner.TableViewModel.Columns.Move(currentIndex, targetIndex);
                    }
                }
            }
        }

        private static bool AreColumnOrdersDifferent(
            List<SchemaDesignerColumn> sourceColumns,
            List<SchemaDesignerColumn> targetColumns)
        {
            var sourceIdsInTargetOrder = sourceColumns
                .Where(sourceColumn => targetColumns.Any(targetColumn => targetColumn.Id == sourceColumn.Id))
                .Select(column => column.Id)
                .ToList();

            var targetExistingIds = targetColumns
                .Where(targetColumn => sourceColumns.Any(sourceColumn => sourceColumn.Id == targetColumn.Id))
                .Select(column => column.Id)
                .ToList();

            return !sourceIdsInTargetOrder.SequenceEqual(targetExistingIds);
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

            if (viewModel.CanEditIsComputed && sourceColumn.IsComputed != targetColumn.IsComputed)
            {
                viewModel.IsComputed = targetColumn.IsComputed;
                viewModel.ComputedFormula = targetColumn.ComputedFormula;
                viewModel.IsComputedPersisted = targetColumn.ComputedPersisted;
                if (viewModel.CanEditIsComputedPersistedNullable)
                {
                    viewModel.IsComputedPersistedNullable = targetColumn.IsNullable;
                }
                return;
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
        /// Processes all foreign key changes across schemas - adding new, modifying existing, and dropping removed foreign keys.
        /// </summary>
        private static void ProcessForeignKeyChanges(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
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
                        AddForeignKeysToTableDesigner(tableDesigner, targetTable, targetTable.ForeignKeys, updatedSchema);
                    }
                }
                else
                {
                    // For existing tables, process foreign key changes
                    UpdateTableForeignKeys(sourceTable, targetTable, schemaDesigner, updatedSchema);
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
            SchemaDesignerModel schema)
        {
            foreach (var fk in foreignKeys)
            {
                tableDesigner.TableViewModel.ForeignKeys.AddNew();
                var sdForeignKey = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                SetForeignKeyProperties(sdForeignKey, table, fk, schema);
            }
        }

        /// <summary>
        /// Sets properties on a foreign key view model from a schema foreign key
        /// </summary>
        private static void SetForeignKeyProperties(
            ForeignKeyViewModel viewModel,
            SchemaDesignerTable sourceTable,
            SchemaDesignerForeignKey fk,
            SchemaDesignerModel schema)
        {
            viewModel.Name = fk.Name;

            var referencedTable = schema.Tables?.FirstOrDefault(table =>
                string.Equals(table.Id.ToString(), fk.ReferencedTableId, StringComparison.OrdinalIgnoreCase));

            if (referencedTable != null)
            {
                viewModel.ForeignTable = $"{referencedTable.Schema}.{referencedTable.Name}";
            }

            viewModel.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(fk.OnDeleteAction);
            viewModel.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(fk.OnUpdateAction);

            viewModel.Columns.Clear();

            if (fk.ColumnsIds != null && fk.ReferencedColumnsIds != null)
            {
                int mappingCount = Math.Min(fk.ColumnsIds.Count, fk.ReferencedColumnsIds.Count);

                for (int i = 0; i < mappingCount; i++)
                {
                    var sourceColumn = sourceTable.Columns?.FirstOrDefault(column =>
                        string.Equals(column.Id.ToString(), fk.ColumnsIds[i], StringComparison.OrdinalIgnoreCase));

                    var referencedColumn = referencedTable?.Columns?.FirstOrDefault(column =>
                        string.Equals(column.Id.ToString(), fk.ReferencedColumnsIds[i], StringComparison.OrdinalIgnoreCase));

                    if (sourceColumn?.Name == null || referencedColumn?.Name == null)
                    {
                        continue;
                    }

                    viewModel.AddNewColumnMapping();
                    viewModel.UpdateColumn(i, sourceColumn.Name);
                    viewModel.UpdateForeignColumn(i, referencedColumn.Name);
                }
            }
        }

        /// <summary>
        /// Update foreign keys in an existing table
        /// </summary>
        private static void UpdateTableForeignKeys(
            SchemaDesignerTable initialTable,
            SchemaDesignerTable updatedTable,
            DacSchemaDesigner designer,
            SchemaDesignerModel schema)
        {
            if (initialTable.ForeignKeys == null && updatedTable.ForeignKeys == null) return;

            // Remove foreign keys that don't exist in the target
            if (initialTable.ForeignKeys != null)
            {
                for (int i = initialTable.ForeignKeys.Count - 1; i >= 0; i--)
                {
                    var sourceFk = initialTable.ForeignKeys[i];
                    if (updatedTable.ForeignKeys == null || !updatedTable.ForeignKeys.Any(fk => fk.Id == sourceFk.Id))
                    {
                        var tableDesigner = designer.GetTableDesigner(updatedTable.Schema, updatedTable.Name);
                        tableDesigner.TableViewModel.ForeignKeys.RemoveAt(i);
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
                        var tableDesigner = designer.GetTableDesigner(updatedTable.Schema, updatedTable.Name);
                        // Add new foreign key
                        tableDesigner.TableViewModel.ForeignKeys.AddNew();
                        var newFk = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                        SetForeignKeyProperties(newFk, updatedTable, targetFk, schema);
                    }
                    else if (!SchemaDesignerUtils.DeepCompareForeignKey(sourceFk, targetFk))
                    {
                        var tableDesigner = designer.GetTableDesigner(updatedTable.Schema, updatedTable.Name);
                        // Update existing foreign key
                        int index = tableDesigner.TableViewModel.ForeignKeys.Items.ToList().FindIndex(fk => fk.Name == sourceFk.Name);
                        if (index >= 0)
                        {
                            var viewModel = tableDesigner.TableViewModel.ForeignKeys.Items[index];
                            SetForeignKeyProperties(viewModel, updatedTable, targetFk, schema);
                        }
                    }
                }
            }
        }
    }
}