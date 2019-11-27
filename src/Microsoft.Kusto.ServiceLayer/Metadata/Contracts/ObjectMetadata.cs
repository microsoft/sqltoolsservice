//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.Metadata.Contracts
{
    /// <summary>
    /// Metadata type enumeration
    /// </summary>
    public enum MetadataType
    {
        Table = 0,
        View = 1,
        SProc = 2,
        Function = 3,
        Schema = 4,
        Database = 5
    }

    /// <summary>
    /// Object metadata information
    /// </summary>
    public class ObjectMetadata 
    {
        public MetadataType MetadataType { get; set; }
    
        public string MetadataTypeName { get; set; }

        public string Name { get; set; }

        public string PrettyName { get; set; }
        
        public string Urn { get; set; }
    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class DatabaseMetadata : ObjectMetadata
    {
        public string ClusterName { get; set; }
    }

    /// <summary>
    /// Database metadata information
    /// </summary>
    public class TableMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
    }

    /// <summary>
    /// Column metadata information
    /// </summary>
    public class ColumnMetadata : TableMetadata
    {
        public string TableName { get; set; }
        public string DataType { get; set; }
    }
}
