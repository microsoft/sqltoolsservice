//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerModelProvider
    {
        /// <summary>
        /// Get the schema model for the database
        /// </summary>
        /// <param name="connectionUri"> The connection URI </param>
        /// <returns></returns>
        public static async Task<SchemaDesignerModel> GetSchemaModel(string connectionUri)
        {
            SchemaDesignerModel schema = new SchemaDesignerModel();
            Dictionary<string, SchemaDesignerTable> tableDict = new Dictionary<string, SchemaDesignerTable>();
            List<IList<DbCellValue>> tables = await SchemaDesignerQueryExecution.RunSimpleQuery(connectionUri, TableAndColumnQuery);
            List<IList<DbCellValue>> relationships = await SchemaDesignerQueryExecution.RunSimpleQuery(connectionUri, RelationshipQuery);
            for (int i = 0; i < tables.Count; i++)
            {
                IList<QueryExecution.Contracts.DbCellValue> row = tables[i];
                string tableName = row[0].DisplayValue;
                string schemaName = row[1].DisplayValue;
                string columnName = row[2].DisplayValue;
                string dataType = row[3].DisplayValue;
                string maxLength = row[4].DisplayValue;
                string precision = row[5].DisplayValue;
                string scale = row[6].DisplayValue;
                string isNullable = row[7].DisplayValue;
                string isPrimaryKey = row[8].DisplayValue;
                string isUnique = row[9].DisplayValue;
                string collation = row[10].DisplayValue;
                string isIdentity = row[11].DisplayValue;
                string seedValue = row[12].DisplayValue;
                string incrementValue = row[13].DisplayValue;
                string key = $"[{schemaName}].[{tableName}]";
                if (!tableDict.ContainsKey(key))
                {
                    tableDict[key] = new SchemaDesignerTable
                    {
                        Id = Guid.NewGuid(),
                        Schema = schemaName,
                        Name = tableName,
                        Columns = new List<SchemaDesignerColumn>(),
                        ForeignKeys = new List<SchemaDesignerForeignKey>()
                    };
                }
                tableDict[key].Columns.Add(new SchemaDesignerColumn
                {
                    Id = Guid.NewGuid(),
                    Name = columnName,
                    DataType = dataType,
                    MaxLength = maxLength == null ? (int?)null : int.Parse(maxLength),
                    Precision = precision == null ? (int?)null : int.Parse(precision),
                    Scale = scale == null ? (int?)null : int.Parse(scale),
                    IsNullable = isNullable == "1",
                    IsPrimaryKey = isPrimaryKey == "1",
                    IsUnique = isUnique == "1",
                    Collation = collation,
                    IsIdentity = isIdentity == "1",
                    IdentitySeed = seedValue == "NULL" ? (int?)null : int.Parse(seedValue),
                    IdentityIncrement = incrementValue == "NULL" ? (int?)null : int.Parse(incrementValue)
                });
            }
            for (int i = 0; i < relationships.Count; i++)
            {
                IList<QueryExecution.Contracts.DbCellValue> row = relationships[i];
                string schemaName = row[1].DisplayValue;
                string tableName = row[2].DisplayValue;
                string key = $"[{schemaName}].[{tableName}]";
                if (!tableDict.ContainsKey(key))
                {
                    continue;
                }
                else
                {
                    SchemaDesignerTable table = tableDict[key];
                    table.ForeignKeys ??= new List<SchemaDesignerForeignKey>();
                    table.ForeignKeys.Add(new SchemaDesignerForeignKey
                    {
                        Id = Guid.NewGuid(),
                        Name = row[0].DisplayValue,
                        Columns = [.. row[3].DisplayValue.Split('|')],
                        ReferencedSchemaName = row[4].DisplayValue,
                        ReferencedTableName = row[5].DisplayValue,
                        ReferencedColumns = [.. row[6].DisplayValue.Split('|')],
                        OnDeleteAction = SchemaDesignerUtils.MapOnAction(row[7].DisplayValue),
                        OnUpdateAction = SchemaDesignerUtils.MapOnAction(row[8].DisplayValue),
                    });
                }
            }

            schema.Tables = [.. tableDict.Values];
            return schema;
        }



        /// <summary>
        /// Get all data types in the database
        /// </summary>
        /// <param name="connectionUri"> The connection URI </param>
        /// <returns></returns>
        public static async Task<List<string>> GetDatatypes(string connectionUri)
        {
            List<IList<DbCellValue>> dataTypes = await SchemaDesignerQueryExecution.RunSimpleQuery(connectionUri, DataTypesQuery);
            List<string> dataTypesList = new List<string>();
            foreach (var row in dataTypes)
            {
                dataTypesList.Add(row[0].DisplayValue);
            }
            return dataTypesList;
        }

        /// <summary>
        /// Get all schema names in the database that are not system schemas
        /// </summary>
        /// <param name="connectionUri"> The connection URI </param>
        /// <returns></returns>
        public static async Task<List<string>> GetSchemas(string connectionUri)
        {
            List<IList<DbCellValue>> schemas = await SchemaDesignerQueryExecution.RunSimpleQuery(connectionUri, SchemaNamesQuery);
            List<string> schemaList = new List<string>();
            foreach (var row in schemas)
            {
                schemaList.Add(row[0].DisplayValue);
            }
            return schemaList;
        }

        /// <summary>
        /// Query to get all relationships in the database
        /// </summary>
        public const string RelationshipQuery = @"
SELECT
    fk.name AS ForeignKeyName,
    SCHEMA_NAME(tp.schema_id) AS SchemaName,
    tp.name AS ParentTable,
    STRING_AGG(cp.name, '|') AS ParentColumns, -- Use | as a separator
    SCHEMA_NAME(tr.schema_id) AS ReferencedSchema,
    tr.name AS ReferencedTable,
    STRING_AGG(cr.name, '|') AS ReferencedColumns, -- Use | as a separator
    fk.delete_referential_action_desc AS OnDeleteAction,
    fk.update_referential_action_desc AS OnUpdateAction
FROM sys.foreign_keys fk
    INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
    INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
    INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
GROUP BY fk.name, tp.schema_id, tp.name, tr.schema_id, tr.name, 
                fk.delete_referential_action_desc, fk.update_referential_action_desc;
        ";

        /// <summary>
        /// Query to get all tables and columns in the database
        /// </summary>
        public const string TableAndColumnQuery = @"
SELECT
    t.name AS TableName,
    s.name AS SchemaName,
    c.name AS ColumnName,
    TYPE_NAME(c.user_type_id) AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
    CASE WHEN uq.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsUnique,
    c.collation_name AS Collation,
    c.is_identity AS IsIdentity,
    id.seed_value AS SeedValue,
    id.increment_value AS IncrementValue
FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    JOIN sys.columns c ON t.object_id = c.object_id
    LEFT JOIN (
                SELECT ic.object_id, ic.column_id
    FROM sys.index_columns ic
        JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
    LEFT JOIN (
                SELECT ic.object_id, ic.column_id
    FROM sys.index_columns ic
        JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    WHERE i.is_unique = 1
            ) uq ON c.object_id = uq.object_id AND c.column_id = uq.column_id
    LEFT JOIN sys.identity_columns id ON  c.object_id = id.object_id AND c.column_id = id.column_id
ORDER BY s.name, t.name, c.column_id;
        ";

        /// <summary>
        /// Query to get all data types in the database
        /// </summary>
        public const string DataTypesQuery = @"
SELECT
    name
FROM sys.types
        ";

        /// <summary>
        /// Query to get all schema names in the database that are not system schemas
        /// </summary>
        public const string SchemaNamesQuery = @"
SELECT
    name
FROM sys.schemas
WHERE name NOT LIKE 'db_%'
        ";
    }
}