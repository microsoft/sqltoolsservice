//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;

namespace Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts
{
    /// <summary>
    /// Types of schema compare endpoints
    /// </summary>
    public enum SchemaCompareEndpointType
    {
        Database = 0,
        Dacpac = 1,
        Project = 2
        // must be kept in-sync with SchemaCompareEndpointType in the MSSQL for VSCode Extension
        // located at extensions\mssql\typings\vscode-mssql.d.ts
    }

    /// <summary>
    /// Info needed from endpoints for schema comparison.
    /// Host-agnostic: does not contain ConnectionDetails (which is ServiceLayer-specific).
    /// Connection resolution is handled by ISchemaCompareConnectionProvider.
    /// </summary>
    public class SchemaCompareEndpointInfo
    {
        /// <summary>
        /// Gets or sets the type of the endpoint
        /// </summary>
        public SchemaCompareEndpointType EndpointType { get; set; }

        /// <summary>
        /// Gets or sets the project file path
        /// </summary>
        public string ProjectFilePath { get; set; }

        /// <summary>
        /// Gets or sets the scripts included in project
        /// </summary>
        public string[] TargetScripts { get; set; }

        /// <summary>
        /// Gets or sets the project data schema provider
        /// </summary>
        public string DataSchemaProvider { get; set; }

        /// <summary>
        /// Gets or sets package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for the database
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Connection owner URI (used by VSCode to look up connections; general-purpose identifier)
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Server name (populated when parsing SCMP files)
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// User name (populated when parsing SCMP files)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Raw connection string (populated when parsing SCMP files)
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Extract target of the project
        /// </summary>
        public DacExtractTarget? ExtractTarget { get; set; }
    }

    /// <summary>
    /// Identifies a schema compare object by name parts and SQL type
    /// </summary>
    public class SchemaCompareObjectId
    {
        /// <summary>
        /// Name to create object identifier
        /// </summary>
        public string[] NameParts;

        /// <summary>
        /// sql object type
        /// </summary>
        public string SqlObjectType;
    }

    /// <summary>
    /// Represents a single difference entry in a schema comparison
    /// </summary>
    public class DiffEntry
    {
        /// <summary>
        /// The schema update action (Create, Delete, Change, etc.) for this difference.
        /// </summary>
        public SchemaUpdateAction UpdateAction { get; set; }

        /// <summary>
        /// The type of schema difference (Object, Property, etc.).
        /// </summary>
        public SchemaDifferenceType DifferenceType { get; set; }

        /// <summary>
        /// The display name of the differing element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Name parts identifying the source object.
        /// </summary>
        public string[] SourceValue { get; set; }

        /// <summary>
        /// Name parts identifying the target object.
        /// </summary>
        public string[] TargetValue { get; set; }

        /// <summary>
        /// The parent diff entry in a hierarchical difference tree.
        /// </summary>
        public DiffEntry Parent { get; set; }

        /// <summary>
        /// Child diff entries under this difference.
        /// </summary>
        public List<DiffEntry> Children { get; set; }

        /// <summary>
        /// The generated SQL script for the source object.
        /// </summary>
        public string SourceScript { get; set; }

        /// <summary>
        /// The generated SQL script for the target object.
        /// </summary>
        public string TargetScript { get; set; }

        /// <summary>
        /// The SQL object type name of the source object.
        /// </summary>
        public string SourceObjectType { get; set; }

        /// <summary>
        /// The SQL object type name of the target object.
        /// </summary>
        public string TargetObjectType { get; set; }

        /// <summary>
        /// Whether this difference is included in the comparison result.
        /// </summary>
        public bool Included { get; set; }
    }

    /// <summary>
    /// Base class for schema compare results
    /// </summary>
    public class SchemaCompareResultBase
    {
        /// <summary>
        /// Whether the operation completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
