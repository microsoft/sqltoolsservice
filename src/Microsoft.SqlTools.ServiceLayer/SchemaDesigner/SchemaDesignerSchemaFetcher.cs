//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public static class SchemaDesignerSchemaFetcher
    {
        public static string GetSchemaModel(string connectionString, string accessToken, string databaseName)
        {
            var builder = ConnectionService.CreateConnectionStringBuilder(connectionString);
            builder.ApplicationName = TableDesignerManager.TableDesignerApplicationNameSuffix;
            builder.InitialCatalog = databaseName;
            return builder.ConnectionString;
        }


        public const string RelationshipQuery = @"
        SELECT 
            fk.name AS ForeignKeyName,
            SCHEMA_NAME(tp.schema_id) AS SchemaName,
            tp.name AS ParentTable, 
            STRING_AGG(cp.name, '|') AS ParentColumns,  -- Use | as a separator
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

        // Query to get all tables and columns in the database
        public const string TableAndColumnQuery = @"
        SELECT 
            SCHEMA_NAME(t.schema_id) AS SchemaName,
            t.name AS TableName,
            c.name AS ColumnName,
            ty.name AS DataType,
            c.is_identity AS IsIdentity,
            CASE 
                WHEN pk.column_id IS NOT NULL THEN 1 
                ELSE 0 
            END AS IsPrimaryKey,
            CASE 
                WHEN fk.column_id IS NOT NULL THEN 1 
                ELSE 0 
            END AS IsForeignKey
        FROM sys.tables t
        JOIN sys.columns c 
            ON t.object_id = c.object_id
        JOIN sys.types ty
            ON c.user_type_id = ty.user_type_id
        LEFT JOIN (
            -- Get primary key columns
            SELECT 
                kc.parent_object_id, 
                ic.column_id
            FROM sys.key_constraints kc
            JOIN sys.index_columns ic 
                ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
            WHERE kc.type = 'PK'
        ) pk 
            ON t.object_id = pk.parent_object_id AND c.column_id = pk.column_id
        LEFT JOIN (
            -- Get foreign key columns
            SELECT 
                fk.parent_object_id, 
                fkc.parent_column_id AS column_id
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc 
                ON fk.object_id = fkc.constraint_object_id
        ) fk 
            ON t.object_id = fk.parent_object_id AND c.column_id = fk.column_id
        WHERE t.type = 'U'
        ";
    }
}