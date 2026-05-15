//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Linq;
using DacSchemaDesigner = Microsoft.Data.Tools.Sql.DesignServices.TableDesigner.SchemaDesigner;
using Microsoft.SqlTools.ServiceLayer.SchemaDesigner;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaDesigner
{
    public class SchemaDesignerAdventureWorksTests
    {
        private const string AdventureWorks2022ConnectionString = "Data Source=localhost\\MSSQLSERVER2025;Initial Catalog=AdventureWorks;Integrated Security=True;Pooling=False;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Name=vscode-mssql;Application Intent=ReadWrite;Command Timeout=30";

        [Test]
        public void DacFxSimpleSchemaLoadsAdventureWorks2022ForeignKeys()
        {
            using (DacSchemaDesigner schemaDesigner = new DacSchemaDesigner(AdventureWorks2022ConnectionString, accessToken: null))
            {
                TestContext.Out.WriteLine($"DacFxAssembly={typeof(DacSchemaDesigner).Assembly.FullName}");
                TestContext.Out.WriteLine($"DacFxAssemblyLocation={typeof(DacSchemaDesigner).Assembly.Location}");

                var simpleSchema = schemaDesigner.SimpleSchema;
                int totalForeignKeys = simpleSchema.Tables.Sum(table => table.ForeignKeys.Count);

                TestContext.Out.WriteLine($"RawDacFx SimpleSchema TableCount={simpleSchema.Tables.Count} TotalForeignKeys={totalForeignKeys}");
                foreach (var table in simpleSchema.Tables.OrderBy(table => table.SchemaName).ThenBy(table => table.Name))
                {
                    TestContext.Out.WriteLine($"RawDacFx Table={table.SchemaName}.{table.Name} ColumnCount={table.Columns.Count} ForeignKeyCount={table.ForeignKeys.Count}");
                    foreach (var foreignKey in table.ForeignKeys.OrderBy(foreignKey => foreignKey.Name))
                    {
                        TestContext.Out.WriteLine(
                            $"RawDacFx FK={table.SchemaName}.{table.Name}.{foreignKey.Name} " +
                            $"Columns=[{string.Join(",", foreignKey.Columns)}] " +
                            $"ReferencedTable={foreignKey.ReferencedTableSchema}.{foreignKey.ReferencedTableName} " +
                            $"ReferencedColumns=[{string.Join(",", foreignKey.ReferencedColumns)}] " +
                            $"IsEnabled={foreignKey.IsEnabled} OnDelete={foreignKey.OnDeleteAction} OnUpdate={foreignKey.OnUpdateAction}");
                    }
                }

                Assert.Greater(simpleSchema.Tables.Count, 0, "AdventureWorks2022 should produce at least one raw DacFx simple schema table.");
                Assert.Greater(totalForeignKeys, 0, "AdventureWorks2022 should produce raw DacFx simple schema foreign keys before SQL Tools projection.");
            }
        }

        [Test]
        public void SchemaDesignerSessionLoadsAdventureWorks2022ForeignKeys()
        {
            using (SchemaDesignerSession session = new SchemaDesignerSession(
                nameof(SchemaDesignerSessionLoadsAdventureWorks2022ForeignKeys),
                AdventureWorks2022ConnectionString,
                accessToken: null))
            {
                var schema = session.InitialSchema;
                var tables = schema.Tables ?? Enumerable.Empty<SchemaDesignerTable>();
                int totalForeignKeys = tables.Sum(table => table.ForeignKeys?.Count ?? 0);

                TestContext.Out.WriteLine($"SQLTools SimpleSchema TableCount={tables.Count()} TotalForeignKeys={totalForeignKeys}");
                foreach (var table in tables.OrderBy(table => table.Schema).ThenBy(table => table.Name))
                {
                    var foreignKeys = table.ForeignKeys ?? Enumerable.Empty<SchemaDesignerForeignKey>();
                    TestContext.Out.WriteLine($"SQLTools Table={table.Schema}.{table.Name} ColumnCount={table.Columns?.Count ?? 0} ForeignKeyCount={foreignKeys.Count()}");

                    foreach (var foreignKey in foreignKeys.OrderBy(foreignKey => foreignKey.Name))
                    {
                        var sourceColumns = foreignKey.ColumnsIds?
                            .Select(columnId => table.Columns?.FirstOrDefault(column => string.Equals(column.Id.ToString(), columnId, StringComparison.OrdinalIgnoreCase))?.Name ?? columnId)
                            .ToList();
                        var referencedTable = tables.FirstOrDefault(candidate => string.Equals(candidate.Id.ToString(), foreignKey.ReferencedTableId, StringComparison.OrdinalIgnoreCase));
                        var referencedColumns = foreignKey.ReferencedColumnsIds?
                            .Select(columnId => referencedTable?.Columns?.FirstOrDefault(column => string.Equals(column.Id.ToString(), columnId, StringComparison.OrdinalIgnoreCase))?.Name ?? columnId)
                            .ToList();

                        TestContext.Out.WriteLine(
                            $"SQLTools FK={table.Schema}.{table.Name}.{foreignKey.Name} " +
                            $"Columns=[{string.Join(",", sourceColumns ?? Enumerable.Empty<string>())}] " +
                            $"ReferencedTable={referencedTable?.Schema}.{referencedTable?.Name} " +
                            $"ReferencedColumns=[{string.Join(",", referencedColumns ?? Enumerable.Empty<string>())}] " +
                            $"ReferencedTableId={foreignKey.ReferencedTableId ?? "<null>"} " +
                            $"OnDelete={foreignKey.OnDeleteAction} OnUpdate={foreignKey.OnUpdateAction}");
                    }
                }

                Assert.Greater(tables.Count(), 0, "AdventureWorks2022 should produce at least one SQL Tools schema table.");
                Assert.Greater(totalForeignKeys, 0, "AdventureWorks2022 should project at least one foreign key into the SQL Tools schema model.");
            }
        }
    }
}
