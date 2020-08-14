//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Diagram.Contracts
{
    public enum DiagramObject
    {
        Database = 1,
        Schema = 2,
        Table = 3
    }

    public class DiagramRequestParams
    {
        public string OwnerUri { get; set; }
        public string Schema { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string Table { get; set; }
        public DiagramObject DiagramView { get; set; }

    }

    public class DiagramRequestResult
    {
        public IDiagramMetadata DiagramMetadata { get; set; }
    }

    public class DiagramModelRequest
    {
        public static readonly
            RequestType<DiagramRequestParams, DiagramRequestResult> Type =
                RequestType<DiagramRequestParams, DiagramRequestResult>.Create("diagram/model");
    }

    public class DiagramPropertiesRequest
    {
        public static readonly
            RequestType<DiagramRequestParams, DiagramRequestResult> Type =
                RequestType<DiagramRequestParams, DiagramRequestResult>.Create("diagram/properties");
    }

    public interface IDiagramMetadata
    {
        public string Name { get; set; }
    }

    public class DatabaseMetadata : IDiagramMetadata
    {
        public string Name { get; set; }
        public string DatabaseID { get; set; }
        public string Size { get; set; }
        public string CreateDate { get; set; }
        public string UserAccess { get; set; }
        public DbSchemasRow[] SchemasData { get; set; }
        public DbTablesRow[] TablesData { get; set; }
    }

    public class SchemaMetadata : IDiagramMetadata
    {
        public string Name { get; set; }
        public SchemaTablesRow[] TablesData { get; set; }
    }

    public class TableMetadata : IDiagramMetadata
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public TableKeysRow[] KeysData { get; set; }
        public TableColumnsRow[] ColumnsData { get; set; }
        public TableRelationshipsRow[] RelationshipsData { get; set; }
    }


    public class DbSchemasRow
    {
        public string SchemaName { get; set; }
        public string SchemaOwner { get; set; }
        public string SchemaID { get; set; }
    }

    public class DbTablesRow
    {
        public string TableName { get; set; }
        public string TableSchema { get; set; }
        public string RowCount { get; set; }
        public string Size { get; set; }
    }

    public class SchemaTablesRow
    {
        public string TableName { get; set; }
        public string RowCount { get; set; }
        public string Size { get; set; }
    }

    public class TableKeysRow
    {
        public string KeyType { get; set; }
        public string KeyName { get; set; }
    }

    public class TableColumnsRow
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
    }

    public class TableRelationshipsRow
    {
        public string ReferencingTable { get; set; }
        public string ReferencingColumn { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
        public string Constraint { get; set; }
    }

}

