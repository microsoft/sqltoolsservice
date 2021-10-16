//------------------------------------------------------------------------------
// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common
{
    /// <summary>
    /// Constants for Common
    /// </summary>
    internal static class SqlCommonConstants
    {
        public static readonly Guid ServerExplorerDataSource = new Guid("{067EA0D9-BA62-43f7-9106-34930C60C528}");
        public static readonly Guid ServerExplorerDataProvider = new Guid("{91510608-8809-4020-8897-FBA057E22D54}");

        // Guids for IHostShutdownService
        public const string HostShutdownService = "008BBF6E-A99D-4ECF-9308-B98E9A8F9F59";
        public static readonly Guid HostShutdownServiceGuid = new Guid("{" + HostShutdownService + "}");

        // Guids for IVsDatabaseEvents
        public const string DatabaseEvents = "0001F087-0DD0-417F-BC26-96FD1A2768BD";
        public static readonly Guid DatabaseEventsGuid = new Guid("{" + HostShutdownService + "}");

        public const int MAX_PATH = 260,    /* max. length of full pathname */
            MAX_DRIVE = 3,  /* max. length of drive component */
            MAX_DIR = 256,  /* max. length of path component */
            MAX_FNAME = 256,    /* max. length of file name component */
            MAX_EXT = 256;  /* max. length of extension component */

        /// <summary>
        /// Used to specify the database version as an int 120, 110, 100, 90, 80
        /// </summary>
        public static class DBVersion
        {
            public const Int32 DBVersion150 = 150;
            public const Int32 DBVersion140 = 140;
            public const Int32 DBVersion130 = 130;
            public const Int32 DBVersion120 = 120;
            public const Int32 DBVersion110 = 110;
            public const Int32 DBVersion100 = 100;
            public const Int32 DBVersion90 = 90;
            public const Int32 DBVersion80 = 80;
            public const Int32 DBVersionAzure = 1025;
            public const Int32 DBVersionAzureV12 = 1200;
        }

        /// <summary>
        /// Used to specify the database version
        /// </summary>
        public enum SqlServerVersion
        {
            Sql2005,
            Sql2008,
            Sql2012,
            Sql2014,
            Sql2016,
            Sql2017,
            Sql2019,
            SqlAzure,
            SqlAzureV12,
            SqlAzureDw
        }

        /// <summary>
        /// Maps constants for commdlg.h
        /// </summary>
        [Flags]
        internal enum FolderBrowseDialogOptions : uint
        {
            None = 0,
            Readonly = 0x00000001,
            OverwritePrompt = 0x00000002,
            HideReadonly = 0x00000004,
            NoChangeDir = 0x00000008,
            ShowHelp = 0x00000010,
            AllowMultiSelect = 0x00000200,
            ExtensionDifferent = 0x00000400,
            PathMustExist = 0x00000800,
            FileMustExist = 0x00001000,
            CreatePrompt = 0x00002000,
            NoAddAllFilesFilter = 0x40000000,   // don't add *.* to filter
        }

        public static class ExceptionData
        {
            public static string FileName = "FileName";
        }

        public const int Sql2005ServerMajorVersion = 9;

        public const string UniqueMonikerBeginning = "__SQL";

        public const string MSSqlUrlScheme = "MSSQL::";

        public const string SafeMSSqlUrlScheme = "MSSQL__";

        public const string MSSqlClrUrlScheme = "MSSQLCLR::";

        public const string AutoCreatedLocalRouteName = "AutoCreatedLocal";

        public const string DeploymentSuffix = "_Deployment";

        public const string BuiltInsReferenceFileName = "BuiltIns";

        public const string TSQLName = "T-SQL";

        public const string DefaultNewProjectName = "Database1";

        public const int QueryStatusTimeOut = 3; // 3 seconds

        /// <summary>
        /// The maximum length of an identifier in Sql Server
        /// </summary>
        public const Int32 MaxIdentifierLength = 128;

        public const int MaxInitialCatalogLength = 128;

        public static readonly char IllegalNtfsReplacementChar = '_';
        public const string Dot = ".";
        public const char DotAsChar = '.';
        public const string Underscore = "_";
        public const string False = "False";
        public const string True = "True";
        public const string NewLine = "\r\n"; // Environment.NewLine is not a constant

        public const string TemplateLanguage = "SQLDB";

        #region Event Log IDs
        public const string EventLogNameApplication = "Application";
        public const int RefactorOperationEventLogId = 1001;
        public const int SchemaObjectsFeatureEventLogId = 1002;
        #endregion

        #region Upgrading

        internal const string ProjectElementName = "Project";
        internal const string ToolsVersion = "ToolsVersion";
        // Starting with Dev16, the recommended value is "Current" (instead of 15.0, 4.0, etc..)
        internal const string DefaultMSBuildToolset = "Current";
        internal const string MSBuildDefinitionUri = @"http://schemas.microsoft.com/developer/msbuild/2003";
        internal const string VSTemplateUri = @"http://schemas.microsoft.com/developer/vstemplate/2005";
        internal const string VSTemplateNS = "vstemplns";
        internal const string xpathWizardDataPrefix = @"/vstemplns:VSTemplate/vstemplns:WizardData/vstemplns:";
        internal const string xpathWizardExtensionPrefix = @"/vstemplns:VSTemplate/vstemplns:WizardExtension/vstemplns:";
        internal const string xpathWizardProjectItemPrefix = @"/vstemplns:VSTemplate/vstemplns:TemplateContent/vstemplns:";
        public const string ExtensionsDir = @"Extensions";
        public const string FullClassName = @"FullClassName";

        internal const string ProjectNS = "ns";
        internal const string ProjectNSXPath = @"//ns:";
        internal const string ProjectNSXPathWithContext = @"./ns:";
        internal const string ProjFileConfigSectionsXPath = "PropertyGroup[@Condition]";
        internal const string ProjFileConfigSectionAttributeName = "Condition";
        internal const string ProjFileConfigSectionAttributeValue = "$(Configuration)";
        internal const string ProjFileNoConfigSectionsXPath = "PropertyGroup";

        public const string EqualEqual = "==";
        public const string Quote = "'";
        public const string DoubleQuote = "\"";
        public const string CmdLine_TokenPrepend = "$(";
        public const string CmdLine_TokenPostpend = ")";

        internal const uint ProjectUpgradeRequired = unchecked((uint)0x01); // __PROJECT_UPGRADE_INFO_FLAGS.epuifProjectUpgradeRequired
        internal const uint SideBySideBackupSupported = unchecked((uint)0x02); // __PROJECT_UPGRADE_INFO_FLAGS.epuifSideBySideBackupSupported
        //internal static uint CopyBackupSupported = unchecked((uint)0x04); // __PROJECT_UPGRADE_INFO_FLAGS.epuifCopyBackupSupported
        //internal static uint BackupSupported = unchecked((uint)0x80); // __PROJECT_UPGRADE_INFO_FLAGS.epuifBackupSupported
        #endregion

        #region Xml Serialization
        /// <summary>
        /// Constant strings for XmlSerialization
        /// </summary>
        internal const string XmlSerialization_DatabaseRefactoringReportUri = "http://schemas.microsoft.com/VisualStudio/2006/DatabaseRefactoringReport";
        internal const string XmlSerialization_DatabaseMSBuildTasksUri = "http://schemas.microsoft.com/VisualStudio/2006/DatabaseMSBuildTasks";
        internal const string XmlSerialization_DBProExtensionsUri = "http://schemas.microsoft.com/VisualStudio/2006/DBProExtensions";
        #endregion

        #region Settings
        internal const string Settings_EntryElement = "Entry";
        internal const string Settings_KeyAttribute = "key";
        internal const string Settings_ValueAttribute = "value";
        internal const string Settings_RootElement = "Dictionary";
        internal const string Settings_ExtensionMappingsAttribute = "ExtensionMappings";
        #endregion

        #region Registry
        public const string RegistrySubKeySQLDB = "SQLDB";
        public const string RegistrySubKeyDataProject = "DataProject";
        public const string RegistrySubKeyDatabase = "Database";

        public const string RegistrySubKeyDialogPage = @"DialogPage\";
        public const string RegistryKeyDataConnectionOptionsSettings = "Microsoft.VisualStudio.Data.Tools.Package.ToolsOptions.DatabaseErrorsAndWarnings.DatabaseErrorsAndWarningsOptionsSettings";
        public const string RegistryKeyDataSchemaCompareSettings = "Microsoft.VisualStudio.Data.Tools.Package.SchemaCompare.SchemaCompareSettings";
        public const string SqlExpressEdition = "Express Edition";
        public const string Local = "(local)";
        public const string DefaultInstanceNameInRegistry = "MSSQLSERVER";

        // Database Registry settings
        public const string RegistryLockTimeout = "LockTimeoutSeconds";
        public const string RegistryQueryTimeout = "QueryTimeoutSeconds";
        public const string RegistryLongRunningQueryTimeout = "LongRunningQueryTimeoutSeconds";
        public const int DefaultSqlQueryTimeout = 60;
        public const int DefaultSqlLockTimeout = 5;
        public const int DefaultSqlLongRunningQueryTimeout = 0;

        // MEF Extension Manager Registry settings
        public const string RegistrySubKeyTestConditionLookupPath = "TestConditionPath";

        #endregion

        #region Version
        internal const string VersionNumberFormatting = "{0}.{1}.{2}.{3}";
        #endregion

        #region ApplicationNames
        internal const string ProductName = "VSDB";
        internal const string ExecutionEngineApplicationName = ProductName + " " + "SqlCmd";
        internal const string SqlEditorApplicationName = ProductName + " " + "T-SQL Editor";
        internal const string EventLogSourceName = "Microsoft Visual Studio - " + ProductName;
        #endregion

        // Used for database references
        public const string ReferencePath = "ReferencePath";
        public const string Include = "Include";

        internal const string Metadata_FullPath = "FullPath";

        /// <summary>
        /// This is used by DTE to override properties during project load.
        /// </summary>
        public const string Reg_NewProjectOverriddenProperties = "NewProjectOverriddenProperties";

        public const string FolderName_Scripts = "Scripts";
        public const string PublishFileName = "Publish";

        public const string MasterDatabaseName = "master";
        public const string MsdbDatabaseName = "msdb";
        public const string TempDatabaseName = "tempdb";
        public const string ModelDatabaseName = "model";

        //used by Add System Database Reference
        internal const string Sql2019SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\150\SqlSchemas";
        internal const string Sql2017SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\140\SqlSchemas";
        internal const string Sql2016SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\130\SqlSchemas";
        internal const string Sql2014SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\120\SqlSchemas";
        internal const string Sql2012SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\110\SqlSchemas";
        internal const string Sql2008SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\100\SqlSchemas";
        internal const string Sql2005SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\90\SqlSchemas";
        internal const string SqlAzureSqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\Azure\SqlSchemas";
        internal const string SqlAzureV12SqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\AzureV12\SqlSchemas";
        internal const string SqlAzureDwSqlSchemaPath = @"Extensions\Microsoft\SQLDB\Extensions\SqlServer\AzureDw\SqlSchemas";
        internal const string MasterDatabaseFileName = "master.dacpac";
        internal const string MsdbDatabaseFileName = "msdb.dacpac";

        public const string SqlFamilyName = "sql";

        public const string MasterKey = "MasterKey";
        public const string DatabaseKey = "DatabaseKey";

        public const string MSDatabaseToolsExtendedPropertyPrefix = "microsoft_database_tools";
        public const string DeployStampPropertyName = MSDatabaseToolsExtendedPropertyPrefix + "_deploystamp";

        // The error code raised by the deployment script when it encounters 
        // a stamp that indicates we are re-deploying a previously deployed script
        public const byte Deploy_AlreadyDeployedState = 100;
        public const int Deploy_AlreadyDeployedSeverity = 16;
        public const int Deploy_UserDefinedNumber = 50000;

        public const string DataSourceSqlClient = "System.Data.SqlClient";

        #region Extensibility constants
        public const string DefaultExtensionsFileName = "Microsoft.Data.Tools.Schema.Extensions.xml";
        public const string ExtensionsNamespace = "urn:Microsoft.Data.Tools.Schema.Extensions";
        public const string ExtensionsXsdFile = "Microsoft.Data.Tools.Schema.Extensions.xsd";
        public const string ExtensionsNamespacePrefix = "ext";
        public const string ExtensionXPath = "/ext:extensions/ext:extension";
        public const string ExtensionsXPath = "/ext:extensions";
        public const string DefaultConfigurationRelativeXPath = "./ext:defaultConfiguration";
        public const string ConfigurationRelativeXPath = "./ext:configuration";
        public const string WizardRelativeXPath = "./ext:wizard";

        public const String ExtensionsXmlSearchPattern = "*.extensions.xml";

        public const string GeneratorSubDir = "Generators";
        public const string SchemaFileSubDir = @"..\Packages\schemas\xml";
        public const string ProductDir = @"ProductDir";
        public const string SQLDBSubDir = RegistrySubKeySQLDB;
        public const string SetupVS = @"Setup\VS";

        #endregion

        #region Configuration Constants

        /// <summary>
        /// Name of the general configuration file.
        /// </summary>
        public const string GeneralConfigFileName = "Microsoft.Data.Tools.Schema.Config.xml";

        #endregion  Configuration Constants

        #region Project Build Actions
        public const String BuildAction_Build = "Build";
        public const String BuildAction_None = "None";
        public const String BuildAction_RefactorLog = "RefactorLog";
        public const String BuildAction_BuildExtensionConfiguration = "BuildExtensionConfiguration";
        public const String BuildAction_DeploymentExtensionConfiguration = "DeploymentExtensionConfiguration";
        public const String BuildAction_PreDeploy = "PreDeploy";
        public const String BuildAction_PostDeploy = "PostDeploy";
        public const String BuildAction_Compile = "Compile";
        public const String BuildAction_Import = "Import";
        #endregion

        #region New Database Wizard constants and properties
        //properties and constants used for Database wizard, although some constants already appearing 
        //somewhere else are being used as is and are not duplicated here
        internal const string DefaultCollation = "SQL_Latin1_General_CP1_CI_AS";
        #endregion //New Database Wizard constants and properties

        #region Build Filenames

        // Keep these value in sync with Microsoft.Data.Tools.Schema.SqlTasks.targets

        public const string MiscellaneousFiles = "MiscellaneousFiles";
        public const string ModelOutputFile = "model.xml";
        public const string ModelOutputFileUriString = "/model.xml";
        public const string PostDeployOutputFile = "postdeploy.sql";
        public const string PreDeployOutputFile = "predeploy.sql";
        public const string RefactorLogOutputFile = "refactor.xml";
        public const string LogicalObjectStreamFile = "LogicalObjectStream.xml";
        public const string PhysicalObjectStreamFile = "PhysicalObjectStream.xml";
        public const string DacMetadataFile = "DacMetadata.xml";
        public const string BacpacMetadata = "BacpacMetadata.xml";
        public const string DacOriginFile = "Origin.xml";
        #endregion

        #region Refactor Log Constants
        public const string XmlElement_Operations = "Operations";
        public const string XmlElement_Operation = "Operation";
        public const string XmlElement_Property = "Property";
        public const string XmlAttribute_Name = "Name";
        public const string XmlAttribute_Key = "Key";
        public const string XmlAttribute_ChangeDateTime = "ChangeDateTime";
        public const string XmlAttribute_Value = "Value";
        public const string XmlAttribute_Version = "Version";
        public const string RefactoringLogFileVersion = "1.0";
        public const string DeploymentConfig = "DeploymentConfig";
        #endregion

        #region VS Constants

        // See env\inc\OMGlyphs.h
        public const int OM_GLYPH_ACC_TYPE_COUNT = 6;
        public const int OM_GLYPH_ERROR = 31;
        public const int OM_GLYPH_CLASS = OM_GLYPH_ACC_TYPE_COUNT * 0;
        public const int OM_GLYPH_CSHARPFILE = OM_GLYPH_ACC_TYPE_COUNT * OM_GLYPH_ERROR + 18;
        public const int OM_GLYPH_REFERENCE = OM_GLYPH_ACC_TYPE_COUNT * OM_GLYPH_ERROR + 22;
        public const int OM_GLYPH_VBPROJECT = OM_GLYPH_ACC_TYPE_COUNT * OM_GLYPH_ERROR + 8;

        #endregion

        public const string IdentitySeedExpressionScript = "IdentitySeedExpressionScript";
        public const string IdentityIncrementExpressionScript = "IdentityIncrementExpressionScript";

        public const bool AssemblyIsModelAware_DefaultValue = false;
        public const bool AssemblySkipCreationIfEmpty_DefaultValue = false;
        public const bool AssemblyIsVisible_DefaultValue = true;
        public const bool AssemblyIsCheckingDataDisabled_DefaultValue = false;
        public const bool GenerateSqlClrDdl_DefaultValue = false;

        public const string RegistrySQLServerTools = @"SQL Server Tools";
        public const string SqlProject_ToolsOptions_General = @"General";
        public const string SqlProject_ToolsOptions_OnlineEditing = @"Online Editing";

        internal const string Dac = "DAC";
        internal const string PreviousDAC = "PreviousDAC";
        internal const string Import_File_Extension = "dacpac";
        internal const string Compact_File_Extension = ".sqlce";
        internal const string Dac_File_Extension = ".dacpac";

        public const string SqlDbInstallDirectory = @"\Extensions\Microsoft\SQLDB";
        internal const string DefaultTestConditionExtensionsDirectory = @"\Extensions\Microsoft\SQLDB\TestConditions";
        public const string SqlStudioProjectTemplate = @"ProjectItems\SSDT.vstemplate";
        public const string DacFxInstallDirectory = @"\Extensions\Microsoft\SQLDB\DAC\150";

        public const string IntermediateTargetFullFileName = "IntermediateTargetFullFileName";

        public const string ProjectOptionOn = "On";
        public const string ProjectOptionOff = "Off";
        public const string ProjectOptionCompareBinary = "Binary";
        public const string ProjectOptionCompareText = "Text";

        public const string CDataString = @"<![CDATA[{0}]]>";

        public const string CreatedDateFieldName = "CreatedDate";
        public const string IsNullableFieldName = "IsNullable";
        public const string IsSequenceExhaustedFieldName = "IsExhausted";
        public const string SequenceCurrentValueFieldName = "CurrentValue";
        public const string IsPrimaryKeyFieldName = "IsPrimaryKey";
        public const string IsForeignKeyFieldName = "IsForeignKey";

        public const string SqlAzureEditionName = "SQL Azure";
        public const string ExpressEditionName = "Express";

        public const string SqlStudioCollectionBasePath = @"\SSDT";
        public const string SqlServerObjectExplorerInitCollection = "SqlServerObjectExplorerInit";
        public const string InitializedSqlServerObjectExplorer = "InitializedSqlServerObjectExplorer";
        public const string guidSqlServerObjectExplorerNeedsInitializationString = "27F27371-B70C-4B7B-BA28-9EDF8A3F0538";
        public static readonly Guid guidSqlServerObjectExplorerNeedsInitialization = new Guid("{" + guidSqlServerObjectExplorerNeedsInitializationString + "}");

        public const string ManagementModelSchema100 = "http://schemas.microsoft.com/sqlserver/ManagementModel/Serialization/2009/08";
        public const string RelationalEngineSchema100 = "http://schemas.microsoft.com/sqlserver/RelationalEngine/Serialization/2009/08";

        public const string ManagementModelSchema105 = "http://schemas.microsoft.com/sqlserver/ManagementModel/Serialization/2009/11";
        public const string RelationalEngineSchema105 = "http://schemas.microsoft.com/sqlserver/RelationalEngine/Serialization/2009/11";

        public const string ManagementModelSchema110 = "http://schemas.microsoft.com/sqlserver/ManagementModel/Serialization/2010/11";
        public const string RelationalEngineSchema110 = "http://schemas.microsoft.com/sqlserver/RelationalEngine/Serialization/2010/11";

        public const string ManagementModelSchema200 = "http://schemas.microsoft.com/sqlserver/ManagementModel/Serialization/2011/03";
        public const string RelationalEngineSchema200 = "http://schemas.microsoft.com/sqlserver/RelationalEngine/Serialization/2011/03";

        public const string ManagementModelSchema250 = "http://schemas.microsoft.com/sqlserver/ManagementModel/Serialization/2011/11";

        public const string ProductSchema300 = "http://schemas.microsoft.com/sqlserver/dac/Serialization/2012/02";
        public const string DriftReportSchema300 = "http://schemas.microsoft.com/sqlserver/dac/DriftReport/2012/02";
        public const string DeployReportSchema300 = "http://schemas.microsoft.com/sqlserver/dac/DeployReport/2012/02";

        public const string CurrentProductSchema = ProductSchema300;
        public const string CurrentDriftReportSchema = DriftReportSchema300;
        public const string CurrentDeployReportSchema = DeployReportSchema300;

        public const string Instances = "Instances";
        public const string MM = "MM";
        public const string RE = "RE";

        public const string IsClusteredIndexColumnAnnotationName = "IsClusteredIndexColumn";

        public const string DatabaseAdvancedOption = "DATABASE";

        public static class SqlBuiltInTypeNames
        {
            public const string Bigint = "bigint";
            public const string Binary = "binary";
            public const string Bit = "bit";
            public const string Char = "char";
            public const string Cursor = "cursor";
            public const string Date = "date";
            public const string Datetime = "datetime";
            public const string Datetime2 = "datetime2";
            public const string DatetimeOffset = "datetimeoffset";
            public const string Decimal = "decimal";
            public const string Float = "float";
            public const string Image = "image";
            public const string Int = "int";
            public const string Money = "money";
            public const string Nchar = "nchar";
            public const string Ntext = "ntext";
            public const string Numeric = "numeric";
            public const string Nvarchar = "nvarchar";
            public const string Real = "real";
            public const string Rowversion = "rowversion";
            public const string Smalldatetime = "smalldatetime";
            public const string Smallint = "smallint";
            public const string Smallmoney = "smallmoney";
            public const string Sql_variant = "sql_variant";
            public const string Table = "table";
            public const string Text = "text";
            public const string Time = "time";
            public const string Timestamp = "timestamp";
            public const string Tinyint = "tinyint";
            public const string Uniqueidentifier = "uniqueidentifier";
            public const string Varbinary = "varbinary";
            public const string Varchar = "varchar";
            public const string Xml = "xml";
        }

        #region system date functions

        public const String DateAdd = "DATEADD";
        public const String DateDiff = "DATEDIFF";
        public const String DateName = "DATENAME";
        public const String DatePart = "DATEPART";

        public static HashSet<String> SystemDateFunctions = new HashSet<String>(System.StringComparer.OrdinalIgnoreCase)
        {
            DateAdd,
            DateDiff,
            DateName,
            DatePart,
        };

        #endregion

        #region Dac Versions

        internal static readonly Version DacVersion100 = new Version(1, 0, 0, 0);
        internal static readonly Version DacVersion105 = new Version(1, 0, 5, 0);
        internal static readonly Version DacVersion110 = new Version(1, 1, 0, 0);
        internal static readonly Version DacVersion200 = new Version(2, 0, 0, 0);
        internal static readonly Version DacVersion250 = new Version(2, 5, 0, 0);
        internal static readonly Version DacVersion300 = new Version(3, 0, 0, 0);
        internal static readonly Version DacVersion310 = new Version(3, 1, 0, 0);
        internal static readonly Version DacVersion320 = new Version(3, 2, 0, 0);

        #endregion
    }
}
