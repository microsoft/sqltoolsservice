//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Diagram.Contracts;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Diagram
{
    /// <summary>
    /// Main class for Diagram Service functionality
    /// </summary>
    public sealed class DiagramService
    {
        private static readonly Lazy<DiagramService> LazyInstance = new Lazy<DiagramService>(() => new DiagramService());

        public static DiagramService Instance => LazyInstance.Value;

        internal static Task DiagramModelTask { get; set; }

        private static ConnectionService connectionService = null;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Initializes the Metadata Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(DiagramModelRequest.Type, HandleDiagramModelRequest);
        }

        /// <summary>
        /// Handle a metadata query request
        /// </summary>        
        internal static async Task HandleDiagramModelRequest(
            DiagramRequestParams metadataParams,
            RequestContext<DiagramRequestResult> requestContext)
        {
            try
            {
                Func<Task> requestHandler = async () =>
                {
                    ConnectionInfo connInfo;
                    DiagramService.ConnectionServiceInstance.TryFindConnection(
                        metadataParams.OwnerUri,
                        out connInfo);

                    string databaseName = metadataParams.Database;
                    string schemaName = metadataParams.Database;
                    string tableName = metadataParams.Database;
                    var dbMetadata = new DatabaseMetadata();
                    var schemaMetadata = new SchemaMetadata();
                    var tableMetadata = new TableMetadata();
                    if (connInfo != null)
                    {
                        using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, "DiagramModel"))
                        {
                            switch (metadataParams.DiagramView)
                            {

                                case (DiagramObject.Database):
                                    ReadDbMetadata(sqlConn, databaseName, dbMetadata);
                                    await requestContext.SendResult(new DiagramRequestResult
                                    {
                                        DiagramMetadata = dbMetadata
                                    });
                                    break;
                                case (DiagramObject.Schema):
                                    ReadSchemaMetadata(sqlConn, schemaName, schemaMetadata);
                                    await requestContext.SendResult(new DiagramRequestResult
                                    {
                                        DiagramMetadata = schemaMetadata
                                    });
                                    break;
                                case (DiagramObject.Table):
                                    ReadTableMetadata(sqlConn, databaseName, tableMetadata);
                                    await requestContext.SendResult(new DiagramRequestResult
                                    {
                                        DiagramMetadata = tableMetadata
                                    });
                                    break;
                                default:
                                    ReadDbMetadata(sqlConn, databaseName, dbMetadata);
                                    await requestContext.SendResult(new DiagramRequestResult
                                    {
                                        DiagramMetadata = dbMetadata
                                    });
                                    break;
                            }
                        }
                    }

                };

                Task task = Task.Run(async () => await requestHandler()).ContinueWithOnFaulted(async t =>
                {
                    await requestContext.SendError(t.Exception.ToString());
                });

                DiagramModelTask = task;
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }

        internal static bool IsSystemDatabase(string database)
        {
            // compare against master for now
            return string.Compare("master", database, StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static void ReadDbMetadata(SqlConnection sqlConn, string databaseName,
        DatabaseMetadata dbMetadata)
        {
            string properties_sql =
                @"SELECT
                        b.database_id,
                        SUM(a.size * 8/1024) 'Size (MB)',
                        b.create_date,
                        b.user_access_desc
                    FROM sys.master_files a 
                    INNER JOIN sys.databases b
                    ON a.database_id = b.database_id
                    WHERE DB_NAME(a.database_id) = '{databaseName}' AND b.name = '{databaseName}'
                    GROUP BY b.database_id, b.create_date, b.user_access_desc
                    ";

            using (SqlCommand propertiesSqlCommand = new SqlCommand(properties_sql, sqlConn))
            {
                using (var reader = propertiesSqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var databaseID = reader[0] as string;
                        var size = reader[1] as string;
                        var createDate = reader[2] as string;
                        var userAccess = reader[3] as string;
                        dbMetadata.DatabaseID = databaseID;
                        dbMetadata.Size = size;
                        dbMetadata.CreateDate = createDate;
                        dbMetadata.UserAccess = userAccess;
                    }


                }
            }

            string schema_sql =
                @"SELECT SCHEMA_NAME, SCHEMA_OWNER, SCHEMA_ID(SCHEMA_NAME) AS SCHEMA_ID
                FROM INFORMATION_SCHEMA.SCHEMATA
                ORDER BY SCHEMA_ID ";

            using (SqlCommand schemaSqlCommand = new SqlCommand(schema_sql, sqlConn))
            {
                using (var reader = schemaSqlCommand.ExecuteReader())
                {
                    var dbSchemasRows = new List<DbSchemasRow>();
                    while (reader.Read())
                    {
                        var schemaName = reader[0] as string;
                        var schemaOwner = reader[1] as string;
                        var schemaID = reader[2] as string;
                        dbSchemasRows.Add(new DbSchemasRow
                        {
                            SchemaName = schemaName,
                            SchemaOwner = schemaOwner,
                            SchemaID = schemaID
                        });
                    }
                    dbMetadata.SchemasData = dbSchemasRows.ToArray();
                }
            }

            string tableSql =
                @"SELECT
                    t.NAME AS TableName,
                    s.Name AS SchemaName,
                    p.rows AS 'RowCount',
                    SUM(a.used_pages) * 8 AS UsedSpaceKB
                FROM
                    sys.tables t
                INNER JOIN
                    sys.indexes i ON t.OBJECT_ID = i.object_id
                INNER JOIN
                    sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                INNER JOIN
                    sys.allocation_units a ON p.partition_id = a.container_id
                LEFT OUTER JOIN
                    sys.schemas s ON t.schema_id = s.schema_id
                GROUP BY
                    t.Name, s.Name, p.Rows
                ORDER BY
                    t.Name";

            using (SqlCommand tableSqlCommand = new SqlCommand(tableSql, sqlConn))
            {
                using (var reader = tableSqlCommand.ExecuteReader())
                {
                    var dbTablesItems = new List<DbTablesRow>();
                    while (reader.Read())
                    {
                        var tableName = reader[0] as string;
                        var tableSchema = reader[1] as string;
                        var rowCount = reader[2] as string;
                        var size = reader[3] as string;
                        dbTablesItems.Add(new DbTablesRow
                        {
                            TableName = tableName,
                            TableSchema = tableName,
                            RowCount = rowCount,
                            Size = size
                        });
                    }
                    dbMetadata.TablesData = dbTablesItems.ToArray();
                }
            }
        }


        internal static void ReadSchemaMetadata(SqlConnection sqlConn, string schemaName,
        SchemaMetadata schemaMetadata)
        {
            string tables_sql =
                @"SELECT
                        t.NAME AS TableName,
                        p.rows AS 'RowCount',
                        SUM(a.used_pages) * 8 AS UsedSpaceKB
                    FROM
                        sys.tables t
                    INNER JOIN
                        sys.indexes i ON t.OBJECT_ID = i.object_id
                    INNER JOIN
                        sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                    INNER JOIN
                        sys.allocation_units a ON p.partition_id = a.container_id
                    LEFT OUTER JOIN
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE s.name = '{schemaName}'
                    GROUP BY
                        t.Name, s.Name, p.Rows
                    ORDER BY
                        t.Name
                    ";

            using (SqlCommand tablesSqlCommand = new SqlCommand(tables_sql, sqlConn))
            {
                using (var reader = tablesSqlCommand.ExecuteReader())
                {
                    var schemaTablesItems = new List<SchemaTablesRow>();
                    while (reader.Read())
                    {
                        var tableName = reader[0] as string;
                        var rowCount = reader[1] as string;
                        var size = reader[2] as string;
                        schemaTablesItems.Add(new SchemaTablesRow
                        {
                            TableName = tableName,
                            RowCount = rowCount,
                            Size = size
                        });
                    }
                    schemaMetadata.TablesData = schemaTablesItems.ToArray();


                }
            }
        }

        internal static void ReadTableMetadata(SqlConnection sqlConn, string tableName,
        TableMetadata tableMetadata)
        {
            string keys_sql =
                @"SELECT 
                        TC.CONSTRAINT_TYPE,
                        column_name as PRIMARYKEYCOLUMN
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 

                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                        ON (TC.CONSTRAINT_TYPE = 'PRIMARY KEY' OR TC.CONSTRAINT_TYPE = 'FOREIGN KEY')
                        AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                        AND KU.table_name='{tableName}'

                    ORDER BY 
                        KU.TABLE_NAME
                        ,KU.ORDINAL_POSITION
                    ;
                    ";

            using (SqlCommand keysSqlCommand = new SqlCommand(keys_sql, sqlConn))
            {
                using (var reader = keysSqlCommand.ExecuteReader())
                {
                    var tableKeysItems = new List<TableKeysRow>();
                    while (reader.Read())
                    {
                        var keyType = reader[0] as string;
                        var keyName = reader[1] as string;
                        tableKeysItems.Add(new TableKeysRow
                        {
                            KeyType = keyType,
                            KeyName = keyName
                        });
                    }
                    tableMetadata.KeysData = tableKeysItems.ToArray();
                }

            }

            string columns_sql =
                @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE INFORMATION_SCHEMA.COLUMNS.TABLE_NAME = '{tableName}'";

            using (SqlCommand columnsSqlCommand = new SqlCommand(columns_sql, sqlConn))
            {
                using (var reader = columnsSqlCommand.ExecuteReader())
                {
                    var tableColumnsItems = new List<TableColumnsRow>();
                    while (reader.Read())
                    {
                        var columnName = reader[0] as string;
                        var columnType = reader[1] as string;
                        tableColumnsItems.Add(new TableColumnsRow
                        {
                            ColumnName = columnName,
                            ColumnType = columnType
                        });
                    }
                    tableMetadata.ColumnsData = tableColumnsItems.ToArray();
                }
            }

            string relationships_sql =
                @"SELECT
                    cu.TABLE_NAME AS ReferencingTable,
                    ku.TABLE_NAME AS ReferencedTable,
                    c.CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS c
                INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu
                ON cu.CONSTRAINT_NAME = c.CONSTRAINT_NAME
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                ON ku.CONSTRAINT_NAME = c.UNIQUE_CONSTRAINT_NAME
                WHERE ku.TABLE_NAME = '{tableName}' OR cu.TABLE_NAME = '{tableName}'
";

            using (SqlCommand relationshipsSqlCommand = new SqlCommand(relationships_sql, sqlConn))
            {
                using (var reader = relationshipsSqlCommand.ExecuteReader())
                {
                    var tableRelationshipsItems = new List<TableRelationshipsRow>();
                    while (reader.Read())
                    {
                        var referencingTable = reader[0] as string;
                        var referencingColumn = reader[1] as string;
                        var referencedTable = reader[2] as string;
                        var referencedColumn = reader[3] as string;
                        var constraint = reader[4] as string;

                        tableRelationshipsItems.Add(new TableRelationshipsRow
                        {
                            ReferencingTable = referencedTable,
                            ReferencingColumn = referencingColumn,
                            ReferencedTable = referencedTable,
                            ReferencedColumn = referencedColumn,
                            Constraint = constraint
                        });
                    }
                    tableMetadata.RelationshipsData = tableRelationshipsItems.ToArray();
                }
            }


        }

















    }
}

