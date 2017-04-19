//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Defines the scripting options.
    /// </summary>
    public class ScriptOptions
    {
        /// <summary>
        /// Generate ANSI padding statements
        /// </summary>
        public string ScriptAnsiPadding { get; set; }

        /// <summary>
        /// Append the generated script to a file
        /// </summary>
        public string AppendToFile { get; set; }

        /// <summary>
        /// Check that an object with the given name exists before dropping or altering or that an object with the given name does not exist before creating.
        /// </summary>
        public string CheckForObjectExistence { get; set; }

        /// <summary>
        /// Continue to script if an error occurs. Otherwise, stop.
        /// </summary>
        public string ContinueScriptingOnError { get; set; }

        /// <summary>
        /// Convert user-defined data types to base types.
        /// </summary>
        public string ConvertUDDTToBaseType { get; set; }

        /// <summary>
        /// Generate script for dependent objects for each object scripted.
        /// </summary>
        public string GenerateScriptForDependentObjects { get; set; }

        /// <summary>
        /// Include descriptive headers for each object generated.
        /// </summary>
        public string IncludeDescriptiveHeaders { get; set; }

        /// <summary>
        /// Include system generated constraint names to enforce declarative referential integrity.
        /// </summary>
        public string IncludeSystemConstraintNames { get; set; }

        /// <summary>
        /// Include statements in the script that are not supported on the specified SQL Server database engine type.
        /// </summary>
        public string IncludeUnsupportedStatements { get; set; }

        /// <summary>
        /// Prefix object names with the object schema.
        /// </summary>
        public string SchemaQualify { get; set; }

        /// <summary>
        /// Script options to set bindings option.
        /// </summary>
        public string Bindings { get; set; }

        /// <summary>
        /// Script the objects that use collation.
        /// </summary>
        public string Collation { get; set; }

        /// <summary>
        /// Script the default values.
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Script Object CREATE/DROP statements.
        /// </summary>
        public string ScriptCreateDrop { get; set; }

        /// <summary>
        /// Script the Extended Properties for each object scripted.
        /// </summary>
        public string ScriptExtendedProperties { get; set; }

        /// <summary>
        /// Script only features compatible with the specified version of SQL Server.
        /// </summary>
        public string ScriptForServerVersion { get; set; }

        /// <summary>
        /// Script only features compatible with the specified SQL Server database engine edition.
        /// </summary>
        public string TargetDatabaseEngineEdition { get; set; }

        /// <summary>
        /// Script only features compatible with the specified SQL Server database engine type.
        /// </summary>
        public string TargetDatabaseEngineType { get; set; }

        /// <summary>
        /// Script all logins available on the server. Passwords will not be scripted.
        /// </summary>
        public string ScriptLogins { get; set; }

        /// <summary>
        /// Generate object-level permissions.
        /// </summary>
        public string ScriptObjectLevelPermissions { get; set; }

        /// <summary>
        /// Script owner for the objects.
        /// </summary>
        public string ScriptOwner { get; set; }

        /// <summary>
        /// Script statistics, and optionally include histograms, for each selected table or view.
        /// </summary>
        public string ScriptStatistics { get; set; }

        /// <summary>
        /// Generate USE DATABASE statement.
        /// </summary>
        public string ScriptUseDatabase { get; set; }

        /// <summary>
        /// Generate script that contains schema only or schema and data.
        /// </summary>
        public string TypeOfDataToScript { get; set; }

        /// <summary>
        /// Scripts the change tracking information.
        /// </summary>
        public string ScriptChangeTracking { get; set; }

        /// <summary>
        /// Script the check constraints for each table or view scripted.
        /// </summary>
        public string ScriptCheckConstraints { get; set; }

        /// <summary>
        /// Scripts the data compression information.
        /// </summary>
        public string ScriptDataCompressionOptions { get; set; }

        /// <summary>
        /// Script the foreign keys for each table scripted.
        /// </summary>
        public string ScriptForeignKey { get; set; }

        /// <summary>
        /// Script the full-text indexes for each table or indexed view scripted.
        /// </summary>
        public string ScriptFullTextIndexes { get; set; }

        /// <summary>
        /// Script the indexes (including XML and clustered indexes) for each table or indexed view scripted.
        /// </summary>
        public string ScriptIndexes { get; set; }

        /// <summary>
        /// Script the primary keys for each table or view scripted
        /// </summary>
        public string ScriptPrimaryKeys { get; set; }

        /// <summary>
        /// Script the triggers for each table or view scripted
        /// </summary>
        public string ScriptTriggers { get; set; }

        /// <summary>
        /// Script the unique keys for each table or view scripted.
        /// </summary>
        public string UniqueKeys { get; set; }
    }
}