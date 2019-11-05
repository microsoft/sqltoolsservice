//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// A wrpaper of ScriptOptions to map the option name with the oen in SMO.ScriptingOptions
    /// </summary>
    public class ScriptAsOptions : ScriptOptions
    {
        public ScriptAsOptions(ScriptOptions scriptOptions)
        {
            Validate.IsNotNull(nameof(scriptOptions), scriptOptions);
            ScriptOptions = scriptOptions;
        }

        public ScriptOptions ScriptOptions { get; private set; }

        /// <summary>
        /// Generate ANSI padding statements
        /// </summary>
        public override bool? ScriptAnsiPadding
        {
            get
            {
                return ScriptOptions.ScriptAnsiPadding;
            }
            set
            {
                ScriptOptions.ScriptAnsiPadding = value;
            }
        }


        /// <summary>
        /// Append the generated script to a file
        /// </summary>
        public override bool? AppendToFile
        {
            get
            {
                return ScriptOptions.AppendToFile;
            }
            set
            {
                ScriptOptions.AppendToFile = value;
            }
        }

        /// <summary>
        /// Continue to script if an error occurs. Otherwise, stop.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public override bool? ContinueScriptingOnError
        {
            get
            {
                return ScriptOptions.ContinueScriptingOnError;
            }
            set
            {
                ScriptOptions.ContinueScriptingOnError = value;
            }
        }

        /// <summary>
        /// Convert user-defined data types to base types.
        /// </summary>
        public override bool? ConvertUDDTToBaseType
        {
            get
            {
                return ScriptOptions.ConvertUDDTToBaseType;
            }
            set
            {
                ScriptOptions.ConvertUDDTToBaseType = value;
            }
        }

        /// <summary>
        /// Generate script for dependent objects for each object scripted.
        /// </summary>
        /// <remarks>
        /// The default is false.
        /// </remarks>
        public override bool? GenerateScriptForDependentObjects
        {
            get
            {
                return ScriptOptions.GenerateScriptForDependentObjects;
            }
            set
            {
                ScriptOptions.GenerateScriptForDependentObjects = value;
            }
        }

        /// <summary>
        /// Include descriptive headers for each object generated.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public override bool? IncludeDescriptiveHeaders
        {
            get
            {
                return ScriptOptions.IncludeDescriptiveHeaders;
            }
            set
            {
                ScriptOptions.IncludeDescriptiveHeaders = value;
            }
        }

        /// <summary>
        /// Check that an object with the given name exists before dropping or altering or that an object with the given name does not exist before creating.
        /// </summary>
        public override bool? IncludeIfNotExists
        {
            get
            {
                return ScriptOptions.IncludeIfNotExists;
            }
            set
            {
                ScriptOptions.IncludeIfNotExists = value;
            }
        }

        /// <summary>
        /// Script options to set vardecimal storage format.
        /// </summary>
        public override bool? IncludeVarDecimal
        {
            get
            {
                return ScriptOptions.IncludeVarDecimal;
            }
            set
            {
                ScriptOptions.IncludeVarDecimal = value;
            }
        }

        /// <summary>
        /// Include system generated constraint names to enforce declarative referential integrity.
        /// </summary>
        public override bool? ScriptDriIncludeSystemNames
        {
            get
            {
                return ScriptOptions.ScriptDriIncludeSystemNames;
            }
            set
            {
                ScriptOptions.ScriptDriIncludeSystemNames = value;
            }
        }

        /// <summary>
        /// Include statements in the script that are not supported on the specified SQL Server database engine type.
        /// </summary>
        public override bool? IncludeUnsupportedStatements
        {
            get
            {
                return ScriptOptions.IncludeUnsupportedStatements;
            }
            set
            {
                ScriptOptions.IncludeUnsupportedStatements = value;
            }
        }

        /// <summary>
        /// Prefix object names with the object schema.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public override bool? SchemaQualify
        {
            get
            {
                return ScriptOptions.SchemaQualify;
            }
            set
            {
                ScriptOptions.SchemaQualify = value;
            }
        }

        /// <summary>
        /// Script options to set bindings option.
        /// </summary>
        public override bool? Bindings
        {
            get
            {
                return ScriptOptions.Bindings;
            }
            set
            {
                ScriptOptions.Bindings = value;
            }
        }

        /// <summary>
        /// Script the objects that use collation.
        /// </summary>
        public override bool? Collation
        {
            get
            {
                return ScriptOptions.Collation;
            }
            set
            {
                ScriptOptions.Collation = value;
            }
        }

        /// <summary>
        /// Script the default values.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public override bool? Default
        {
            get
            {
                return ScriptOptions.Default;
            }
            set
            {
                ScriptOptions.Default = value;
            }
        }

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
        public override string ScriptCreateDrop
        {
            get
            {
                return ScriptOptions.ScriptCreateDrop;
            }
            set
            {
                ScriptOptions.ScriptCreateDrop = value;
            }
        }

        /// <summary>
        /// Script the Extended Properties for each object scripted.
        /// </summary>
        /// <remarks>
        /// The default is true.
        /// </remarks>
        public override bool? ScriptExtendedProperties
        {
            get
            {
                return ScriptOptions.ScriptExtendedProperties;
            }
            set
            {
                ScriptOptions.ScriptExtendedProperties = value;
            }
        }

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
        public override string ScriptCompatibilityOption
        {
            get
            {
                return ScriptOptions.ScriptCompatibilityOption;
            }
            set
            {
                ScriptOptions.ScriptCompatibilityOption = value;
            }
        }

        /// <summary>
        /// Script only features compatible with the specified SQL Server database engine type.
        /// Possible Values:
        ///   SingleInstance
        ///   SqlAzure
        /// </summary>
        public override string TargetDatabaseEngineType
        {
            get
            {
                return ScriptOptions.TargetDatabaseEngineType;
            }
            set
            {
                ScriptOptions.TargetDatabaseEngineType = value;
            }
        }

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
        public override string TargetDatabaseEngineEdition
        {
            get
            {
                return ScriptOptions.TargetDatabaseEngineEdition;
            }
            set
            {
                ScriptOptions.TargetDatabaseEngineEdition = value;
            }
        }

        /// <summary>
        /// Script all logins available on the server. Passwords will not be scripted.
        /// </summary>
        public override bool? ScriptLogins
        {
            get
            {
                return ScriptOptions.ScriptLogins;
            }
            set
            {
                ScriptOptions.ScriptLogins = value;
            }
        }

        /// <summary>
        /// Generate object-level permissions.
        /// </summary>
        public override bool? ScriptObjectLevelPermissions
        {
            get
            {
                return ScriptOptions.ScriptObjectLevelPermissions;
            }
            set
            {
                ScriptOptions.ScriptObjectLevelPermissions = value;
            }
        }

        /// <summary>
        /// Script owner for the objects.
        /// </summary>
        public override bool? ScriptOwner
        {
            get
            {
                return ScriptOptions.ScriptOwner;
            }
            set
            {
                ScriptOptions.ScriptOwner = value;
            }
        }

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
        public override string ScriptStatistics
        {
            get
            {
                return ScriptOptions.ScriptStatistics;
            }
            set
            {
                ScriptOptions.ScriptStatistics = value;
            }
        }

        /// <summary>
        /// Generate USE DATABASE statement.
        /// </summary>
        public override bool? ScriptUseDatabase
        {
            get
            {
                return ScriptOptions.ScriptUseDatabase;
            }
            set
            {
                ScriptOptions.ScriptUseDatabase = value;
            }
        }

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
        public override string TypeOfDataToScript
        {
            get
            {
                return ScriptOptions.TypeOfDataToScript;
            }
            set
            {
                ScriptOptions.TypeOfDataToScript = value;
            }
        }

        /// <summary>
        /// Scripts the change tracking information.
        /// </summary>
        public override bool? ScriptChangeTracking
        {
            get
            {
                return ScriptOptions.ScriptChangeTracking;
            }
            set
            {
                ScriptOptions.ScriptChangeTracking = value;
            }
        }

        /// <summary>
        /// Script the check constraints for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public override bool? ScriptCheckConstraints
        {
            get
            {
                return ScriptOptions.ScriptCheckConstraints;
            }
            set
            {
                ScriptOptions.ScriptCheckConstraints = value;
            }
        }

        /// <summary>
        /// Scripts the data compression information.
        /// </summary>
        public override bool? ScriptDataCompressionOptions
        {
            get
            {
                return ScriptOptions.ScriptDataCompressionOptions;
            }
            set
            {
                ScriptOptions.ScriptDataCompressionOptions = value;
            }
        }

        /// <summary>
        /// Script the foreign keys for each table scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public override bool? ScriptForeignKeys
        {
            get
            {
                return ScriptOptions.ScriptForeignKeys;
            }
            set
            {
                ScriptOptions.ScriptForeignKeys = value;
            }
        }

        /// <summary>
        /// Script the full-text indexes for each table or indexed view scripted.
        /// </summary>
        public override bool? ScriptFullTextIndexes
        {
            get
            {
                return ScriptOptions.ScriptFullTextIndexes;
            }
            set
            {
                ScriptOptions.ScriptFullTextIndexes = value;
            }
        }

        /// <summary>
        /// Script the indexes (including XML and clustered indexes) for each table or indexed view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public override bool? ScriptIndexes
        {
            get
            {
                return ScriptOptions.ScriptIndexes;
            }
            set
            {
                ScriptOptions.ScriptIndexes = value;
            }
        }

        /// <summary>
        /// Script the primary keys for each table or view scripted
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public override bool? ScriptPrimaryKeys
        {
            get
            {
                return ScriptOptions.ScriptPrimaryKeys;
            }
            set
            {
                ScriptOptions.ScriptPrimaryKeys = value;
            }
        }

        /// <summary>
        /// Script the triggers for each table or view scripted
        /// </summary>
        public override bool? ScriptTriggers
        {
            get
            {
                return ScriptOptions.ScriptTriggers;
            }
            set
            {
                ScriptOptions.ScriptTriggers = value;
            }
        }

        /// <summary>
        /// Script the unique keys for each table or view scripted.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        /// </remarks>
        public override bool? UniqueKeys
        {
            get
            {
                return ScriptOptions.UniqueKeys;
            }
            set
            {
                ScriptOptions.UniqueKeys = value;
            }
        }

        /// <summary>
        /// Returns Generate ANSI padding statements
        /// </summary>
        public bool? AnsiPadding { get { return ScriptOptions.ScriptAnsiPadding; } }

        /// <summary>
        /// Returns ConvertUDDTToBaseType
        /// </summary>
        public bool? ConvertUserDefinedDataTypesToBaseType { get { return ScriptOptions.ConvertUDDTToBaseType; } }

        /// <summary>
        /// Returns IncludeDescriptiveHeaders
        /// </summary>
        public bool? IncludeHeaders { get { return ScriptOptions.IncludeDescriptiveHeaders; } }


        /// <summary>
        /// Returns ScriptDriIncludeSystemNames
        /// </summary>
        public bool? DriIncludeSystemNames { get { return ScriptOptions.ScriptDriIncludeSystemNames; } }

        /// <summary>
        /// Returns SchemaQualify
        /// </summary>
        public bool? SchemaQualifyForeignKeysReferences { get { return ScriptOptions.SchemaQualify; } }

        /// <summary>
        /// Returns false if Collation is true
        /// </summary>
        public bool? NoCollation { get { return !ScriptOptions.Collation; } }

        /// <summary>
        /// Returns the value of Default Property
        /// </summary>
        public bool? DriDefaults { get { return ScriptOptions.Default; } }

        /// <summary>
        /// Returns the value of ScriptExtendedProperties Property
        /// </summary>
        public bool? ExtendedProperties { get { return ScriptOptions.ScriptExtendedProperties; } }

        /// <summary>
        /// Returns the value of ScriptObjectLevelPermissions Property
        /// </summary>
        public bool? Permissions { get { return ScriptOptions.ScriptObjectLevelPermissions; } }

        /// <summary>
        /// Returns the value of ScriptStatistics Property
        /// </summary>
        public string Statistics { get { return ScriptOptions.ScriptStatistics; } }

        /// <summary>
        /// Returns the value of ScriptChangeTracking Property
        /// </summary>
        public bool? ChangeTracking { get { return ScriptOptions.ScriptChangeTracking; } }


        /// <summary>
        /// Returns the value of ScriptCheckConstraints Property
        /// </summary>
        public bool? DriChecks { get { return ScriptOptions.ScriptCheckConstraints; } }

        /// <summary>
        /// Returns the value of ScriptDataCompressionOptions Property
        /// </summary>
        public bool? ScriptDataCompression { get { return ScriptOptions.ScriptDataCompressionOptions; } }


        /// <summary>
        /// Returns the value of ScriptForeignKeys Property
        /// </summary>
        public bool? DriForeignKeys { get { return ScriptOptions.ScriptForeignKeys; } }


        /// <summary>
        /// Returns the value of ScriptFullTextIndexes Property
        /// </summary>
        public bool? FullTextIndexes { get { return ScriptOptions.ScriptFullTextIndexes; } }


        /// <summary>
        /// Returns the value of ScriptIndexes Property
        /// </summary>
        public bool? Indexes { get { return ScriptOptions.ScriptIndexes; } }


        /// <summary>
        /// Returns the value of ScriptPrimaryKeys Property
        /// </summary>
        public bool? DriPrimaryKey { get { return ScriptOptions.ScriptPrimaryKeys; } }

        /// <summary>
        /// Returns the value of ScriptTriggers Property
        /// </summary>
        public bool? Triggers { get { return ScriptOptions.ScriptTriggers; } }

        /// <summary>
        /// Returns the value of UniqueKeys Property
        /// </summary>
        public bool? DriUniqueKeys { get { return ScriptOptions.UniqueKeys; } }
    }
}
