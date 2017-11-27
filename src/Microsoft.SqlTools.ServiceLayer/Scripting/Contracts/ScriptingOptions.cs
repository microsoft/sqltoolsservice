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
        /// Returns Generate ANSI padding statements
        /// </summary>
        public bool? AnsiPadding { get { return ScriptAnsiPadding; } }

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
        /// Returns ConvertUDDTToBaseType
        /// </summary>
        public bool? ConvertUserDefinedDataTypesToBaseType { get { return ConvertUDDTToBaseType; } }

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
        /// Returns IncludeDescriptiveHeaders
        /// </summary>
        public bool? IncludeHeaders { get { return IncludeDescriptiveHeaders; } }

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
        /// Returns ScriptDriIncludeSystemNames
        /// </summary>
        public bool? DriIncludeSystemNames { get { return ScriptDriIncludeSystemNames; } }

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
        /// Returns SchemaQualify
        /// </summary>
        public bool? SchemaQualifyForeignKeysReferences { get { return SchemaQualify; } }

        /// <summary>
        /// Script options to set bindings option.
        /// </summary>
        public bool? Bindings { get; set; } = false;

        /// <summary>
        /// Script the objects that use collation.
        /// </summary>
        public bool? Collation { get; set; } = false;

        /// <summary>
        /// Returns false if Collation is true
        /// </summary>
        public bool? NoCollation { get { return !Collation; } }

        /// <summary>
        /// Script the default values.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public bool? Default { get; set; } = true;

        /// <summary>
        /// Returns the value of Default Property
        /// </summary>
        public bool? DriDefaults { get { return Default; } }


        /// <summary>
        /// Script Object CREATE/DROP statements.  
        /// Possible values: 
        ///   ScriptCreate
        ///   ScriptDrop
        ///   ScriptCreateDrop
        ///   ScriptSelect
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
        /// Returns the value of ScriptExtendedProperties Property
        /// </summary>
        public bool? ExtendedProperties { get { return ScriptExtendedProperties; } }


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
        /// Script only features compatible with the specified SQL Server database engine type.
        /// Possible Values:
        ///   SingleInstance
        ///   SqlAzure
        /// </summary>
        public string TargetDatabaseEngineType { get; set; } = "SingleInstance";
        
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
        /// Script all logins available on the server. Passwords will not be scripted.
        /// </summary>
        public bool? ScriptLogins { get; set; } = false;

        /// <summary>
        /// Generate object-level permissions.
        /// </summary>
        public bool? ScriptObjectLevelPermissions { get; set; } = false;

        /// <summary>
        /// Returns the value of ScriptObjectLevelPermissions Property
        /// </summary>
        public bool? Permissions { get { return ScriptObjectLevelPermissions; } }

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
        /// Returns the value of ScriptStatistics Property
        /// </summary>
        public string Statistics { get { return ScriptStatistics; } }


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
        /// Returns the value of ScriptChangeTracking Property
        /// </summary>
        public bool? ChangeTracking { get { return ScriptChangeTracking; } }


        /// <summary>
        /// Script the check constraints for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptCheckConstraints { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptCheckConstraints Property
        /// </summary>
        public bool? DriChecks { get { return ScriptCheckConstraints; } }

        /// <summary>
        /// Scripts the data compression information.
        /// </summary>
        public bool? ScriptDataCompressionOptions { get; set; } = false;

        /// <summary>
        /// Returns the value of ScriptDataCompressionOptions Property
        /// </summary>
        public bool? ScriptDataCompression { get { return ScriptDataCompressionOptions; } }


        /// <summary>
        /// Script the foreign keys for each table scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptForeignKeys { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptForeignKeys Property
        /// </summary>
        public bool? DriForeignKeys { get { return ScriptForeignKeys; } }


        /// <summary>
        /// Script the full-text indexes for each table or indexed view scripted.
        /// </summary>
        public bool? ScriptFullTextIndexes { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptFullTextIndexes Property
        /// </summary>
        public bool? FullTextIndexes { get { return ScriptFullTextIndexes; } }


        /// <summary>
        /// Script the indexes (including XML and clustered indexes) for each table or indexed view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptIndexes { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptIndexes Property
        /// </summary>
        public bool? DriIndexes { get { return ScriptIndexes; } }


        /// <summary>
        /// Script the primary keys for each table or view scripted
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? ScriptPrimaryKeys { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptPrimaryKeys Property
        /// </summary>
        public bool? DriPrimaryKey { get { return ScriptPrimaryKeys; } }


        /// <summary>
        /// Script the triggers for each table or view scripted
        /// </summary>
        public bool? ScriptTriggers { get; set; } = true;

        /// <summary>
        /// Returns the value of ScriptTriggers Property
        /// </summary>
        public bool? Triggers { get { return ScriptTriggers; } }


        /// <summary>
        /// Script the unique keys for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public bool? UniqueKeys { get; set; } = true;

        /// <summary>
        /// Returns the value of UniqueKeys Property
        /// </summary>
        public bool? DriUniqueKeys { get { return UniqueKeys; } }

    }
}