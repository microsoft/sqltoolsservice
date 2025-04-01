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
        /// <param name="reportDictionary">The report dictionary to update.</param>
        /// <param name="table">The table being modified.</param>
        /// <param name="tableState">The type of table modification.</param>
        /// <param name="sqlScript">The SQL script for the modification.</param>
        /// <param name="changeDescription">A description of the action performed.</param>
        public static void TrackTableChange(
            Dictionary<string, SchemaDesignerReportObject> reportDictionary,
            SchemaDesignerTable table,
            SchemaDesignerReportTableState tableState,
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
            }
            else
            {
                reportDictionary.Add(table.Id.ToString(), new SchemaDesignerReportObject
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
            DacSchemaDesigner sd)
        {
            // Validate inputs
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            var changeReport = new Dictionary<string, SchemaDesignerReportObject>();

            // Process the schema changes in a specific order to maintain dependencies:
            // 1. Process table structure changes (create, rename, alter, drop)
            // 2. Add new constraints and foreign keys
            // 3. Drop constraints and foreign keys that might block other operations

            // Step 2: Process table additions and modifications
            ProcessNewTables(initialSchema, updatedSchema, changeReport, sd);
            ProcessModifiedTables(initialSchema, updatedSchema, changeReport, sd);

            // Step 3: Process foreign key changes for all tables
            ProcessForeignKeyChanges(initialSchema, updatedSchema, changeReport, sd);

            // Step 1: First identify tables being dropped and handle their foreign keys
            ProcessDroppedTables(initialSchema, updatedSchema, changeReport, sd);

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
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            DacSchemaDesigner sd)
        {
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            if (initialSchema.Tables == null || initialSchema.Tables.Count == 0)
            {
                return;
            }

            if (updatedSchema.Tables == null || updatedSchema.Tables.Count == 0)
            {
                return;
            }

            foreach (var sourceTable in initialSchema.Tables)
            {
                var targetTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == sourceTable.Id);

                if (targetTable == null)
                {

                    // Table has been dropped - first drop all foreign keys
                    if (sourceTable.ForeignKeys != null && sourceTable.ForeignKeys.Count > 0)
                    {
                        foreach (var foreignKey in sourceTable.ForeignKeys)
                        {

                            TrackTableChange(
                                changeReport,
                                sourceTable,
                                SchemaDesignerReportTableState.DROPPED,
                                $"Dropping foreign key '{foreignKey.Name}' that referenced table '{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}'"
                            );

                        }
                    }

                    sd.TablesMarkedForDrop.Add([sourceTable.Schema, sourceTable.Name]);

                    // Now drop the table itself
                    TrackTableChange(
                        changeReport,
                        sourceTable,
                        SchemaDesignerReportTableState.DROPPED,
                        $"Dropping table '{sourceTable.Schema}.{sourceTable.Name}'"
                    );
                }
            }
        }

        /// <summary>
        /// Identifies and processes tables that have been newly added in the target schema.
        /// </summary>
        private static void ProcessNewTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            DacSchemaDesigner sd)
        {
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            if (initialSchema.Tables == null || initialSchema.Tables.Count == 0)
            {
                return;
            }

            if (updatedSchema.Tables == null || updatedSchema.Tables.Count == 0)
            {
                return;
            }

            foreach (var targetTable in updatedSchema.Tables)
            {
                var sourceTable = initialSchema.Tables.FirstOrDefault(t => t.Id == targetTable.Id);

                if (sourceTable == null)
                {
                    // This is a new table - create it with all columns and constraints
                    string creationScript = SchemaCreationScriptGenerator.GenerateTableDefinition(targetTable);

                    var newTable = sd.CreateTable(targetTable.Schema, targetTable.Name);
                    newTable.TableViewModel.Schema = targetTable.Schema;

                    if (targetTable.Columns == null)
                    {
                        continue;
                    }

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

                    TrackTableChange(
                        changeReport,
                        targetTable,
                        SchemaDesignerReportTableState.CREATED,
                        $"Creating new table '{targetTable.Schema}.{targetTable.Name}' with {targetTable.Columns.Count} column(s)"
                    );
                }
            }
        }

        /// <summary>
        /// Identifies and processes tables that exist in both schemas but have modifications.
        /// Handles renames, schema transfers, and column changes.
        /// </summary>
        private static void ProcessModifiedTables(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            DacSchemaDesigner sd)
        {
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            if (initialSchema.Tables == null || initialSchema.Tables.Count == 0)
            {
                return;
            }

            if (updatedSchema.Tables == null || updatedSchema.Tables.Count == 0)
            {
                return;
            }

            foreach (var initialTable in initialSchema.Tables)
            {
                var updatedTable = updatedSchema.Tables.FirstOrDefault(t => t.Id == initialTable.Id);

                if (updatedTable != null && !SchemaDesignerUtils.DeepCompareTable(initialTable, updatedTable))
                {
                    var tableDesigner = sd.GetTableDesigner(initialTable.Schema, initialTable.Name);
                    bool hasModifications = false;

                    // Check for table name change
                    if (initialTable.Name != updatedTable.Name)
                    {
                        TrackTableChange(
                            changeReport,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Renaming table from '{initialTable.Name}' to '{updatedTable.Name}'"
                        );

                        tableDesigner.TableViewModel.Name = updatedTable.Name;
                        hasModifications = true;
                    }

                    // Check for schema change
                    if (initialTable.Schema != updatedTable.Schema)
                    {
                        TrackTableChange(
                            changeReport,
                            updatedTable,
                            SchemaDesignerReportTableState.UPDATED,
                            $"Moving table from schema '{initialTable.Schema}' to schema '{updatedTable.Schema}'"
                        );
                        tableDesigner.TableViewModel.Schema = updatedTable.Schema;
                        hasModifications = true;
                    }

                    // Process column changes
                    GenerateColumnModificationScripts(initialTable, updatedTable, changeReport, tableDesigner);


                    // If there were any modifications, add them to the migration script
                    if (hasModifications)
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
        /// Processes all foreign key changes across schemas - adding new, modifying existing, and dropping removed foreign keys.
        /// </summary>
        private static void ProcessForeignKeyChanges(
            SchemaDesignerModel initialSchema,
            SchemaDesignerModel updatedSchema,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            DacSchemaDesigner sd)
        {
            if (initialSchema == null) throw new ArgumentNullException(nameof(initialSchema));
            if (updatedSchema == null) throw new ArgumentNullException(nameof(updatedSchema));

            if (initialSchema.Tables == null || initialSchema.Tables.Count == 0)
            {
                return;
            }

            if (updatedSchema.Tables == null || updatedSchema.Tables.Count == 0)
            {
                return;
            }

            // First handle foreign keys for new tables
            foreach (var updatedTable in updatedSchema.Tables)
            {
                var initialTable = initialSchema.Tables.FirstOrDefault(t => t.Id == updatedTable.Id);

                if (initialTable == null)
                {
                    // This is a new table - add all its foreign keys
                    if (updatedTable.ForeignKeys != null)
                    {
                        var tableDesigner = sd.GetTableDesigner(updatedTable.Schema, updatedTable.Name);
                        foreach (var foreignKey in updatedTable.ForeignKeys)
                        {
                            string fkScript = SchemaCreationScriptGenerator.GenerateForeignKeyScript(updatedTable, foreignKey);

                            tableDesigner.TableViewModel.ForeignKeys.AddNew();

                            var sdForeignKey = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                            sdForeignKey.Name = foreignKey.Name;
                            sdForeignKey.ForeignTable = $"{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}";
                            sdForeignKey.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(foreignKey.OnDeleteAction);
                            sdForeignKey.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(foreignKey.OnUpdateAction);
                            sdForeignKey.Columns.Clear();
                            if (foreignKey.Columns == null || foreignKey.ReferencedColumns == null)
                            {
                                continue;
                            }
                            for (var i = 0; i < foreignKey.Columns.Count; i++)
                            {
                                sdForeignKey.AddNewColumnMapping();
                                sdForeignKey.UpdateColumn(i, foreignKey.Columns[i]);
                                sdForeignKey.UpdateForeignColumn(i, foreignKey.ReferencedColumns[i]);
                            }

                            TrackTableChange(
                                changeReport,
                                updatedTable,
                                SchemaDesignerReportTableState.CREATED,
                                $"Adding foreign key '{foreignKey.Name}' to reference table '{foreignKey.ReferencedSchemaName}.{foreignKey.ReferencedTableName}'"
                            );

                        }
                    }
                }
                else
                {
                    // Remove foreing keys from initial table that are not in updated table
                    if (initialTable.ForeignKeys != null && initialTable.ForeignKeys.Count > 0)
                    {
                        var tableDesigner = sd.GetTableDesigner(updatedTable.Schema, updatedTable.Name);
                        int index = 0;
                        foreach (var sourceForeignKey in initialTable.ForeignKeys)
                        {
                            var targetForeignKey = updatedTable.ForeignKeys?.FirstOrDefault(fk => fk.Id == sourceForeignKey.Id);

                            if (targetForeignKey == null)
                            {
                                tableDesigner.TableViewModel.ForeignKeys.RemoveAt(index);

                                TrackTableChange(
                                    changeReport,
                                    updatedTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    $"Removing foreign key '{sourceForeignKey.Name}' that referenced table '{sourceForeignKey.ReferencedSchemaName}.{sourceForeignKey.ReferencedTableName}'"
                                );

                            }
                            index++;
                        }

                        index = 0;

                        foreach (var targetForeignKey in updatedTable.ForeignKeys)
                        {
                            var sourceForeignKey = initialTable.ForeignKeys?.FirstOrDefault(fk => fk.Name == targetForeignKey.Name);

                            if (sourceForeignKey == null)
                            {
                                TrackTableChange(
                                    changeReport,
                                    updatedTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    $"Adding new foreign key '{targetForeignKey.Name}' to reference table '{targetForeignKey.ReferencedSchemaName}.{targetForeignKey.ReferencedTableName}'"
                                );

                                tableDesigner.TableViewModel.ForeignKeys.AddNew();
                                var sdForeignKey = tableDesigner.TableViewModel.ForeignKeys.Items.Last();
                                sdForeignKey.Name = targetForeignKey.Name;
                                sdForeignKey.ForeignTable = $"{targetForeignKey.ReferencedSchemaName}.{targetForeignKey.ReferencedTableName}";
                                sdForeignKey.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(targetForeignKey.OnDeleteAction);
                                sdForeignKey.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(targetForeignKey.OnUpdateAction);
                                sdForeignKey.Columns.Clear();

                                for (var i = 0; i < targetForeignKey.Columns.Count; i++)
                                {
                                    sdForeignKey.AddNewColumnMapping();
                                    sdForeignKey.UpdateColumn(i, targetForeignKey.Columns[i]);
                                    sdForeignKey.UpdateForeignColumn(i, targetForeignKey.ReferencedColumns[i]);
                                }
                            }
                            else if (!SchemaDesignerUtils.DeepCompareForeignKey(sourceForeignKey, targetForeignKey))
                            {
                                TrackTableChange(
                                    changeReport,
                                    updatedTable,
                                    SchemaDesignerReportTableState.UPDATED,
                                    $"Modifying foreign key '{targetForeignKey.Name}' ({GetForeignKeyChangeDescription(sourceForeignKey, targetForeignKey)})"
                                );

                                var fk = tableDesigner.TableViewModel.ForeignKeys.Items[index];
                                fk.Name = targetForeignKey.Name;
                                fk.ForeignTable = $"{targetForeignKey.ReferencedSchemaName}.{targetForeignKey.ReferencedTableName}";
                                fk.OnDeleteAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(targetForeignKey.OnDeleteAction);
                                fk.OnUpdateAction = SchemaDesignerUtils.ConvertOnActionToSqlForeignKeyAction(targetForeignKey.OnUpdateAction);
                                fk.Columns.Clear();
                                for (var i = 0; i < targetForeignKey.Columns.Count; i++)
                                {
                                    fk.AddNewColumnMapping();
                                    fk.UpdateColumn(i, targetForeignKey.Columns[i]);
                                    fk.UpdateForeignColumn(i, targetForeignKey.ReferencedColumns[i]);
                                }
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
        /// <param name="initialTable">The original table definition.</param>
        /// <param name="updatedTable">The target table definition.</param>
        /// <param name="changeReport">Report dictionary to update with changes.</param>
        /// <returns>A complete SQL script for all column modifications.</returns>
        private static void GenerateColumnModificationScripts(
            SchemaDesignerTable initialTable,
            SchemaDesignerTable updatedTable,
            Dictionary<string, SchemaDesignerReportObject> changeReport,
            DacTableDesigner tableDesigner)
        {
            if (initialTable == null) throw new ArgumentNullException(nameof(initialTable));
            if (updatedTable == null) throw new ArgumentNullException(nameof(updatedTable));

            if (initialTable.Columns == null || initialTable.Columns.Count == 0)
            {
                return;
            }

            if (updatedTable.Columns == null || updatedTable.Columns.Count == 0)
            {
                return;
            }
            int index = 0;
            // Process columns to drop first
            foreach (var sourceColumn in initialTable.Columns)
            {
                var targetColumn = updatedTable.Columns.FirstOrDefault(c => c.Id == sourceColumn.Id);

                if (targetColumn == null)
                {
                    tableDesigner.TableViewModel.Columns.RemoveAt(index);
                    TrackTableChange(
                        changeReport,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Dropping column '{sourceColumn.Name}' of type '{sourceColumn.DataType}'"
                    );
                }
                index++;
            }

            // Process column additions and modifications
            foreach (var targetColumn in updatedTable.Columns)
            {
                var sourceColumn = initialTable.Columns.FirstOrDefault(c => c.Id == targetColumn.Id);

                TableColumnViewModel? col = tableDesigner.TableViewModel.Columns.Items.FirstOrDefault(c => c.Name == targetColumn.Name);

                if (sourceColumn == null && col == null)
                {
                    TrackTableChange(
                        changeReport,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Adding new column '{targetColumn.Name}' of type '{targetColumn.DataType}'"
                    );

                    tableDesigner.TableViewModel.Columns.AddNew();
                    col = tableDesigner.TableViewModel.Columns.Items[tableDesigner.TableViewModel.Columns.Items.Count - 1];
                    col.Name = targetColumn.Name;
                    col.DataType = targetColumn.DataType;
                    if (col.CanEditLength)
                    {
                        col.Length = targetColumn.MaxLength.ToString();
                    }
                    if (col.CanEditPrecision)
                    {
                        col.Precision = targetColumn.Precision;
                    }
                    if (col.CanEditScale)
                    {
                        col.Scale = targetColumn.Scale;
                    }
                    if (col.CanEditIsNullable)
                    {
                        col.IsNullable = targetColumn.IsNullable;
                    }
                    if (col.CanEditIsIdentity && targetColumn.IsIdentity)
                    {
                        col.IsIdentity = targetColumn.IsIdentity;
                        col.IdentitySeed = targetColumn.IdentitySeed;
                        col.IdentityIncrement = targetColumn.IdentityIncrement;
                    }
                    col.IsPrimaryKey = targetColumn.IsPrimaryKey;
                    if (col.CanEditDefaultValue)
                    {
                        col.DefaultValue = targetColumn.DefaultValue;
                    }
                }
                else if (!SchemaDesignerUtils.DeepCompareColumn(sourceColumn, targetColumn))
                {
                    TrackTableChange(
                        changeReport,
                        updatedTable,
                        SchemaDesignerReportTableState.UPDATED,
                        $"Modifying column '{targetColumn.Name}' ({GetColumnChangeDescription(sourceColumn, targetColumn)})"
                    );

                    col.Name = targetColumn.Name;
                    col.DataType = targetColumn.DataType;
                    if (col.CanEditLength)
                    {
                        col.Length = targetColumn.MaxLength.ToString();
                    }
                    if (col.CanEditPrecision)
                    {
                        col.Precision = targetColumn.Precision;
                    }
                    if (col.CanEditScale)
                    {
                        col.Scale = targetColumn.Scale;
                    }
                    if (col.CanEditIsNullable)
                    {
                        col.IsNullable = targetColumn.IsNullable;
                    }
                    if (col.CanEditIsIdentity && targetColumn.IsIdentity)
                    {
                        col.IsIdentity = targetColumn.IsIdentity;
                        col.IdentitySeed = targetColumn.IdentitySeed;
                        col.IdentityIncrement = targetColumn.IdentityIncrement;
                    }
                    col.IsPrimaryKey = targetColumn.IsPrimaryKey;
                    if (col.CanEditDefaultValue)
                    {
                        col.DefaultValue = targetColumn.DefaultValue;
                    }
                }
            }
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