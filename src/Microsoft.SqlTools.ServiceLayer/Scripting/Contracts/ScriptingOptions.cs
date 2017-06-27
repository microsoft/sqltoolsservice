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
        public bool? ScriptAnsiPadding { get; set; } = false;

        /// <summary>
        /// Append the generated script to a file
        /// </summary>
        public bool? AppendToFile { get; set; } = false;

        /// <summary>
        /// Continue to script if an error occurs. Otherwise, stop.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? ContinueScriptingOnError { get; set; } = true;

        /// <summary>
        /// Convert user-defined data types to base types.
        /// </summary>
        public bool? ConvertUDDTToBaseType { get; set; } = false;

        /// <summary>
        /// Generate script for dependent objects for each object scripted.
        /// </summary>
        /// <remarks>
        /// The default is false.
        /// </remarks>
        public bool? GenerateScriptForDependentObjects { get; set; } = false;

        /// <summary>
        /// Include descriptive headers for each object generated.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? IncludeDescriptiveHeaders { get; set; } = true;

        /// <summary>
        /// Check that an object with the given name exists before dropping or altering or that an object with the given name does not exist before creating.
        /// </summary>
        public bool? IncludeIfNotExists { get; set; } = false;

        /// <summary>
        /// Script options to set vardecimal storage format.
        /// </summary>
        public bool? IncludeVarDecimal { get; set; } = true;

        /// <summary>
        /// Include system generated constraint names to enforce declarative referential integrity.
        /// </summary>
        public bool? ScriptDriIncludeSystemNames { get; set; } = false;

        /// <summary>
        /// Include statements in the script that are not supported on the specified SQL Server database engine type.
        /// </summary>
        public bool? IncludeUnsupportedStatements { get; set; } = true;

        /// <summary>
        /// Prefix object names with the object schema.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? SchemaQualify { get; set; } = true;

        /// <summary>
        /// Script options to set bindings option.
        /// </summary>
        public bool? Bindings { get; set; } = false;

        /// <summary>
        /// Script the objects that use collation.
        /// </summary>
        public bool? Collation { get; set; } = false;

        /// <summary>
        /// Script the default values.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? Default { get; set; } = true;

        /// <summary>
        /// Script Object CREATE/DROP statements.  
        /// Possible values: 
        ///   ScriptCreate
        ///   ScriptDrop
        ///   ScriptCreateDrop
        /// </summary>
        /// <remarks>
        /// The default is ScriptCreate.
        /// </remarks>
        public string ScriptCreateDrop { get; set; } = "ScriptCreate";

        /// <summary>
        /// Script the Extended Properties for each object scripted.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? ScriptExtendedProperties { get; set; } = true;

        /// <summary>
        /// Script only features compatible with the specified version of SQL Server.  Possible values:
        ///   Script90Compat
        ///   Script100Compat
        ///   Script105Compat
        ///   Script110Compat
        ///   Script120Compat
        ///   Script130Compat
        ///   Script140Compat  
        /// </summary>
        /// <remarks>
        /// The default is Script140Compat.
        /// </remarks>
        public string ScriptCompatibilityOption { get; set; } = "Script140Compat";

        /// <summary>
        /// Script only features compatible with the specified SQL Server database engine edition.
        /// Possible Values:
        ///   SqlServerPersonalEdition
        ///   SqlServerStandardEdition 
        ///   SqlServerEnterpriseEdition 
        ///   SqlServerExpressEdition
        ///   SqlAzureDatabaseEdition
        ///   SqlDatawarehouseEdition
        ///   SqlServerStretchEdition 
        /// </summary>
        public string TargetDatabaseEngineEdition { get; set; } = "SqlServerEnterpriseEdition";

        /// <summary>
        /// Script only features compatible with the specified SQL Server database engine type.
        /// Possible Values:
        ///   SingleInstance
        ///   SqlAzure
        /// </summary>
        public string TargetDatabaseEngineType { get; set; } = "SingleInstance";

        /// <summary>
        /// Script all logins available on the server. Passwords will not be scripted.
        /// </summary>
        public bool? ScriptLogins { get; set; } = false;

        /// <summary>
        /// Generate object-level permissions.
        /// </summary>
        public bool? ScriptObjectLevelPermissions { get; set; } = false;

        /// <summary>
        /// Script owner for the objects.
        /// </summary>
        public bool? ScriptOwner { get; set; } = false;

        /// <summary>
        /// Script statistics, and optionally include histograms, for each selected table or view.
        /// Possible values:
        ///   ScriptStatsNone
        ///   ScriptStatsDDL
        ///   ScriptStatsAll
        /// </summary>
        /// <remarks>
        /// The default value is ScriptStatsNone.
        /// </remarks>
        public string ScriptStatistics { get; set; } = "ScriptStatsNone";

        /// <summary>
        /// Generate USE DATABASE statement.
        /// </summary>
        public bool? ScriptUseDatabase { get; set; } = true;

        /// <summary>
        /// Generate script that contains schema only or schema and data.
        /// Possible Values:
        ///   SchemaAndData
        ///   DataOnly
        ///   SchemaOnly
        /// </summary>
        /// <remarks>
        /// The default value is SchemaOnly.
        /// </remarks>
        public string TypeOfDataToScript { get; set; } = "SchemaOnly";

        /// <summary>
        /// Scripts the change tracking information.
        /// </summary>
        public bool? ScriptChangeTracking { get; set; } = false;

        /// <summary>
        /// Script the check constraints for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptCheckConstraints { get; set; } = true;

        /// <summary>
        /// Scripts the data compression information.
        /// </summary>
        public bool? ScriptDataCompressionOptions { get; set; } = false;

        /// <summary>
        /// Script the foreign keys for each table scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptForeignKeys { get; set; } = true;

        /// <summary>
        /// Script the full-text indexes for each table or indexed view scripted.
        /// </summary>
        public bool? ScriptFullTextIndexes { get; set; } = true;

        /// <summary>
        /// Script the indexes (including XML and clustered indexes) for each table or indexed view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptIndexes { get; set; } = true;

        /// <summary>
        /// Script the primary keys for each table or view scripted
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptPrimaryKeys { get; set; } = true;

        /// <summary>
        /// Script the triggers for each table or view scripted
        /// </summary>
        public bool? ScriptTriggers { get; set; } = true;

        /// <summary>
        /// Script the unique keys for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? UniqueKeys { get; set; } = true;
    }
}