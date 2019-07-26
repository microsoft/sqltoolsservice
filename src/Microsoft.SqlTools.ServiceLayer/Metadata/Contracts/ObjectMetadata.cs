//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Metadata.Contracts
{
    /// <summary>
    /// Metadata type enumeration
    /// </summary>
    public enum MetadataType
    {
        Unknown = 0,
        Table = 1,
        View = 2,
        SProc = 3,
        Function = 4,
        Schema = 5,
        Database = 6
    }

    /// <summary>
    /// Extension helper methods for ObjectMetadata types
    /// </summary>
    public static class ObjectMetadataExtensions
    {
        public static MetadataType MetadataType(this NamedSmoObject smoObj)
        {
            if (smoObj is Table)
            {
                return Contracts.MetadataType.Table;
            }
            else if (smoObj is View)
            {
                return Contracts.MetadataType.View;
            }
            else if (smoObj is StoredProcedure)
            {
                return Contracts.MetadataType.SProc;
            }
            else if (smoObj is UserDefinedFunction)
            {
                return Contracts.MetadataType.Function;
            }
            else if (smoObj is Schema)
            {
                return Contracts.MetadataType.Schema;
            }
            else if (smoObj is Database)
            {
                return Contracts.MetadataType.Database;
            }

            return Contracts.MetadataType.Unknown;
        }

        public static string MetadataTypeName(this MetadataType metadataType)
        {
            switch (metadataType)
            {
                case Contracts.MetadataType.Table:
                    return "Table";
                case Contracts.MetadataType.View:
                    return "View";
                case Contracts.MetadataType.SProc:
                    return "StoredProcedure";
                case Contracts.MetadataType.Function:
                    return "UserDefinedFunction";
                case Contracts.MetadataType.Schema:
                    return "Schema";
                case Contracts.MetadataType.Database:
                    return "Database";
            }

            return "Unknown";
        }
    }

    /// <summary>
    /// Object metadata information
    /// </summary>
    public class ObjectMetadata
    {
        public ObjectMetadata() { }

        public ObjectMetadata(NamedSmoObject smoObj)
        {
            MetadataType = smoObj.MetadataType();
            MetadataTypeName = MetadataType.MetadataTypeName();
            Name = smoObj.Name;
            if (smoObj is ScriptSchemaObjectBase schemaSmoObj)
            {
                Schema = schemaSmoObj.Schema;
            }
            try
            {
                if (smoObj.Urn != null)
                {
                    Urn = smoObj.Urn.ToString();
                }
            }
            catch
            {
                // Sometimes URN will throw - just ignore since there's nothing we can do
            }
        }

        public MetadataType MetadataType { get; set; }

        public string MetadataTypeName { get; set; }

        public string Schema { get; set; }

        public string Name { get; set; }

        public string Urn { get; set; }
    }

}
