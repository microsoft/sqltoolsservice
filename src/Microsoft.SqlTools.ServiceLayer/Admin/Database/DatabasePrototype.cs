//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Resources;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;
using AzureEdition = Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper.AzureEdition;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Admin
{
    /// <summary>
    /// Database Prototype
    /// </summary>
    /// <remarks>
    /// This exposes properties common to all server versions.  Properties specific to
    /// versions after 7.0 are in a derived class.
    /// </remarks>
    public class DatabasePrototype : IDynamicValues
    {
        #region data members

        protected class DatabaseData
        {
            public string name;
            public string owner;
            public string collation;

            public RecoveryModel recoveryModel;
            public DateTime lastBackupDate;
            public DateTime lastLogBackupDate;
            public DatabaseUserAccess restrictAccess;
            public DatabaseStatus databaseState;
            public DefaultCursor defaultCursor;
            public CompatibilityLevel databaseCompatibilityLevel;
            public ContainmentType databaseContainmentType;
            public PageVerify pageVerify;
            public AzureEdition azureEdition;
            public string azureEditionDisplayValue;
            public string configuredServiceLevelObjective;
            public string currentServiceLevelObjective;
            public DbSize maxSize;

            public bool closeCursorOnCommit;
            public bool isReadOnly;
            public bool autoClose;
            public bool autoShrink;
            public bool autoCreateStatistics;
            public bool autoCreateStatisticsIncremental;
            public bool autoUpdateStatistics;
            public bool autoUpdateStatisticsAsync;
            public bool ansiNullDefault;
            public bool ansiNulls;
            public bool ansiWarnings;
            public bool ansiPadding;
            public bool arithabort;
            public bool concatNullYieldsNull;
            public bool numericRoundAbort;
            public bool quotedIdentifier;
            public bool recursiveTriggers;
            public bool fullTextIndexingEnabled;
            public bool dbChaining;
            public bool trustworthy;
            public bool dateCorrelationOptimization;
            public bool brokerEnabled;
            public bool parameterization;
            public bool varDecimalEnabled;
            public bool encryptionEnabled;
            public bool honorBrokerPriority;
            public int defaultFulltextLanguageLcid;
            public int defaultLanguageLcid;
            public int twoDigitYearCutoff;
            public int targetRecoveryTime;
            public bool nestedTriggersEnabled;
            public bool transformNoiseWords;
            public bool isReadCommittedSnapshotOn;
            public bool allowSnapshotIsolation;
            public string defaultCollation = string.Empty;
            public System.Guid serviceBrokerGuid;
            public FilestreamNonTransactedAccessType filestreamNonTransactedAccess = FilestreamNonTransactedAccessType.Off;
            public string filestreamDirectoryName = string.Empty;
            public DelayedDurability delayedDurability;

            public MirroringSafetyLevel mirrorSafetyLevel = MirroringSafetyLevel.Off;
            public string witnessServer = string.Empty;

            public bool isSystemDB;

            public bool queryStoreEnabled;

            public int maxDop;
            public int? maxDopForSecondary;
            public DatabaseScopedConfigurationOnOff legacyCardinalityEstimation;
            public DatabaseScopedConfigurationOnOff legacyCardinalityEstimationForSecondary;
            public DatabaseScopedConfigurationOnOff parameterSniffing;
            public DatabaseScopedConfigurationOnOff parameterSniffingForSecondary;
            public DatabaseScopedConfigurationOnOff queryOptimizerHotfixes;
            public DatabaseScopedConfigurationOnOff queryOptimizerHotfixesForSecondary;

            
            /// <summary>
            /// Constructor for new databases using default data
            /// </summary>
            /// <remarks>
            /// This method is only called when the user doesn't have access to the model database
            /// </remarks>
            public DatabaseData(CDataContainer context)
            {      
                this.name = string.Empty;
                this.owner = string.Empty;
                this.restrictAccess = DatabaseUserAccess.Multiple;
                this.isReadOnly = false;
                this.databaseState = DatabaseStatus.Normal;
                this.closeCursorOnCommit = false;
                this.defaultCursor = DefaultCursor.Global;
                this.autoClose = false;
                this.autoShrink = false;
                this.autoCreateStatistics = true;
                this.autoCreateStatisticsIncremental = false;
                this.autoUpdateStatistics = true;
                this.autoUpdateStatisticsAsync = false;
                this.ansiNullDefault = false;
                this.ansiNulls = false;
                this.ansiPadding = false;
                this.ansiWarnings = false;
                this.arithabort = false;
                this.concatNullYieldsNull = false;
                this.numericRoundAbort = false;
                this.quotedIdentifier = false;
                this.recursiveTriggers = false;
                this.recoveryModel = RecoveryModel.Simple;
                this.dbChaining = false;
                this.trustworthy = false;
                this.dateCorrelationOptimization = false;
                this.brokerEnabled = false;
                this.parameterization = false;
                this.varDecimalEnabled = false;
                this.encryptionEnabled = false;
                this.honorBrokerPriority = false;
                this.filestreamNonTransactedAccess = FilestreamNonTransactedAccessType.Off;
                this.filestreamDirectoryName = String.Empty;
                this.delayedDurability = DelayedDurability.Disabled;
                this.azureEdition = AzureEdition.Standard;
                this.azureEditionDisplayValue = AzureEdition.Standard.ToString();
                this.configuredServiceLevelObjective = String.Empty;
                this.currentServiceLevelObjective = String.Empty;
                this.maxSize = new DbSize(0, DbSize.SizeUnits.MB);
                this.maxDop = 0;
                this.maxDopForSecondary = null;
                this.legacyCardinalityEstimation = DatabaseScopedConfigurationOnOff.Off;
                this.legacyCardinalityEstimationForSecondary = DatabaseScopedConfigurationOnOff.Primary;
                this.parameterSniffing = DatabaseScopedConfigurationOnOff.On;
                this.parameterSniffingForSecondary = DatabaseScopedConfigurationOnOff.Primary;
                this.queryOptimizerHotfixes = DatabaseScopedConfigurationOnOff.Off;
                this.queryOptimizerHotfixesForSecondary = DatabaseScopedConfigurationOnOff.Primary;

                //The following properties are introduced for contained databases.
                //In case of plain old databases, these values should reflect the server configuration values.
                this.defaultFulltextLanguageLcid = context.Server.Configuration.DefaultFullTextLanguage.ConfigValue;
                int defaultLanguagelangid = context.Server.Configuration.DefaultLanguage.ConfigValue;
                this.defaultLanguageLcid = 1033; // LanguageUtils.GetLcidFromLangId(context.Server, defaultLanguagelangid);
                this.nestedTriggersEnabled = context.Server.Configuration.NestedTriggers.ConfigValue == 1;
                this.transformNoiseWords = context.Server.Configuration.TransformNoiseWords.ConfigValue == 1;
                this.twoDigitYearCutoff = context.Server.Configuration.TwoDigitYearCutoff.ConfigValue;

                this.targetRecoveryTime = 0;

                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());

                //in katmai var decimal going to be true by default
                if (context.Server.Information.Version.Major >= 10)
                {
                    this.varDecimalEnabled = true;
                }

                if (7 < context.Server.Information.Version.Major)
                {
                    this.collation = this.defaultCollation = manager.GetString("general_default");
                }
                else
                {
                    this.collation = String.Empty;
                }

                if (9 <= context.Server.Information.Version.Major)
                {
                    this.pageVerify = PageVerify.Checksum;
                }
                else
                {
                    this.pageVerify = PageVerify.TornPageDetection;
                }

                // Full-text indexing will always be enabled in Katmai
                if (context.Server.Information.Version.Major <= 9)
                {
                    this.fullTextIndexingEnabled = false;
                }
                else
                {
                    this.fullTextIndexingEnabled = true;
                }

                switch (context.SqlServerVersion)
                {
                    case 6:

                        string errorMessage = manager.GetString("error_60compatibility");
                        throw new InvalidOperationException(errorMessage);

                    case 7:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version70;
                        break;

                    case 8:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version80;
                        break;

                    case 9:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version90;
                        break;

                    case 10:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version100;
                        break;

                    case 11:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version110;
                        break;

                    case 12:

                        this.databaseCompatibilityLevel = CompatibilityLevel.Version120;
                        break;

                    case 13:
                        this.databaseCompatibilityLevel = CompatibilityLevel.Version130;
                        break;

                    case 14:
                        this.databaseCompatibilityLevel = CompatibilityLevel.Version140;
                        break;

                    default:                        
                        this.databaseCompatibilityLevel = CompatibilityLevel.Version140;
                        break;
                }

                if (context.Server.ServerType == DatabaseEngineType.SqlAzureDatabase)
                { //These properties are only available for Azure DBs
                    this.azureEdition = AzureEdition.Standard;
                    this.azureEditionDisplayValue = azureEdition.ToString();
                    this.currentServiceLevelObjective = AzureSqlDbHelper.GetDefaultServiceObjective(this.azureEdition);
                    this.configuredServiceLevelObjective = AzureSqlDbHelper.GetDefaultServiceObjective(this.azureEdition);
                    this.maxSize = AzureSqlDbHelper.GetDatabaseDefaultSize(this.azureEdition);                    
                }
            }

            /// <summary>
            /// Query to get the current and configured SLO for a target DB. Must be ran on the master DB.
            /// </summary>
            private const string dbSloQuery = @"
SELECT so.name as configured_slo_name, so2.name as current_slo_name
FROM dbo.slo_database_objectives do 
    INNER JOIN dbo.slo_service_objectives so ON do.configured_objective_id = so.objective_id
	INNER JOIN dbo.slo_service_objectives so2 ON do.current_objective_id = so2.objective_id
WHERE do.database_id = @DbID
";

            /// <summary>
            /// Constructor for existing databases
            /// </summary>
            public DatabaseData(CDataContainer context, string databaseName)
            {
                // set prototype properties to match the database
                Database db = context.Server.Databases[databaseName];

                if (db == null)
                {
                    context.Server.Databases.Refresh();
                    db = context.Server.Databases[databaseName];
                }

                isSystemDB = db.IsSystemObject;

                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());

                try
                {
                    this.owner = db.Owner;
                }
                catch (Exception)
                {
                    // TODO: fix the exception in SMO
                    this.owner = string.Empty;
                }


                // Databases that are restored from other servers might not have valid owners.
                // If the logged in user is an administrator and the owner is not valid, show
                // the owner as blank.  Note that only administrators can successfully change
                // execution context to dbo to perform the check for an invalid owner.
                if ((9 <= context.SqlServerVersion) && context.LoggedInUserIsSysadmin)
                {
                    try
                    {
                        DataSet dsResult = db.ExecuteWithResults(
                            "select suser_sname((select sid from sys.database_principals where name = N'dbo'));");
                        DataTable tableResult = dsResult.Tables[0];
                        if (tableResult.Rows.Count > 0 && tableResult.Columns.Count > 0)
                        {
                            DataRow rowResult = tableResult.Rows[0];
                            DataColumn colResult = tableResult.Columns[0];
                            if (string.IsNullOrEmpty(rowResult[colResult].ToString()))
                            {
                                this.owner = String.Empty;
                            }
                        }
                        else
                        {
                            this.owner = String.Empty;
                        }

                    }
                    catch (FailedOperationException)
                    {
                        // the owner is invalid, set the owner string to String.Empty
                        this.owner = String.Empty;
                    }
                }

                this.name = databaseName;
                this.restrictAccess = db.DatabaseOptions.UserAccess;

                try
                {
                    this.databaseState = db.Status;
                }
                catch (Exception ex)
                {
                    SqlException sqlException = CUtils.GetSqlException(ex);
                    if (null != sqlException && true == CUtils.IsPermissionDeniedException(sqlException))
                    {
                        this.databaseState = DatabaseStatus.Inaccessible;
                    }
                    else
                    {
                        throw ex;
                    }
                }

                this.closeCursorOnCommit = db.DatabaseOptions.CloseCursorsOnCommitEnabled;
                this.defaultCursor = (db.IsSupportedProperty("LocalCursorsDefault") &&
                                      db.DatabaseOptions.LocalCursorsDefault)
                    ? DefaultCursor.Local
                    : DefaultCursor.Global;
                if (db.IsSupportedProperty("AutoClose"))
                {
                    this.autoClose = db.DatabaseOptions.AutoClose;
                }
                this.autoShrink = db.DatabaseOptions.AutoShrink;
                this.autoCreateStatistics = db.DatabaseOptions.AutoCreateStatistics;
                this.autoUpdateStatistics = db.DatabaseOptions.AutoUpdateStatistics;
                this.ansiNullDefault = db.DatabaseOptions.AnsiNullDefault;
                this.ansiNulls = db.DatabaseOptions.AnsiNullsEnabled;
                this.quotedIdentifier = db.DatabaseOptions.QuotedIdentifiersEnabled;
                this.recursiveTriggers = db.DatabaseOptions.RecursiveTriggersEnabled;
                if (db.IsSupportedProperty("RecoveryModel"))
                {
                    this.recoveryModel = db.DatabaseOptions.RecoveryModel;
                }

                if (db.IsSupportedProperty("LastBackupDate"))
                {
                    this.lastBackupDate = db.LastBackupDate;
                }

                if (db.IsSupportedProperty("LastLogBackupDate"))
                {
                    this.lastLogBackupDate = db.LastLogBackupDate;
                }

                if (Utils.IsSql12OrLater(context.Server.Information.Version.Major))
                {
                    this.autoCreateStatisticsIncremental = db.DatabaseOptions.AutoCreateStatisticsIncremental;
                }

                // SQL Server 2000 options
                if (7 < context.Server.Information.Version.Major)
                {
                    if (context.IsNewObject)
                    {
                        this.collation = this.defaultCollation = manager.GetString("general_default");
                    }
                    else
                    {
                        this.collation = db.Collation;
                    }

                    this.isReadOnly = db.DatabaseOptions.ReadOnly;
                    if (db.IsSupportedProperty("PageVerify"))
                    {
                        this.pageVerify = db.DatabaseOptions.PageVerify;
                    }
                    this.ansiWarnings = db.DatabaseOptions.AnsiWarningsEnabled;
                    this.ansiPadding = db.DatabaseOptions.AnsiPaddingEnabled;
                    this.arithabort = db.DatabaseOptions.ArithmeticAbortEnabled;
                    this.numericRoundAbort = db.DatabaseOptions.NumericRoundAbortEnabled;
                    this.concatNullYieldsNull = db.DatabaseOptions.ConcatenateNullYieldsNull;
                }
                else
                {
                    this.collation = String.Empty;
                    this.isReadOnly = false;
                    this.pageVerify = PageVerify.None;
                    this.ansiPadding = false;
                    this.ansiWarnings = false;
                    this.arithabort = false;
                    this.numericRoundAbort = false;
                    this.concatNullYieldsNull = false;
                }

                // DB_CHAINING supported in SQL Server 2000 SP3 and later
                Version sql2000sp3 = new Version(8, 0, 760);
                if (sql2000sp3 <= context.Server.Information.Version)
                {
                    this.dbChaining = db.DatabaseOptions.DatabaseOwnershipChaining;
                }
                else
                {
                    this.dbChaining = false;
                }

                // SQL Server 2005 options
                if (8 < context.Server.Version.Major)
                {
                    this.autoUpdateStatisticsAsync = db.DatabaseOptions.AutoUpdateStatisticsAsync;
                    this.trustworthy = db.DatabaseOptions.Trustworthy;
                    this.dateCorrelationOptimization = db.DatabaseOptions.DateCorrelationOptimization;
                    this.parameterization = db.DatabaseOptions.IsParameterizationForced;
                    this.isReadCommittedSnapshotOn = db.IsReadCommittedSnapshotOn;
                    if (db.IsSupportedProperty("BrokerEnabled"))
                    {
                        this.serviceBrokerGuid = db.ServiceBrokerGuid;
                        this.brokerEnabled = db.BrokerEnabled;
                    }
                    if (db.IsSupportedProperty("SnapshotIsolationState"))
                    {
                        this.allowSnapshotIsolation = !(db.SnapshotIsolationState == SnapshotIsolationState.Disabled);
                    }
                }
                else
                {
                    this.autoUpdateStatisticsAsync = false;
                    this.trustworthy = false;
                    this.dateCorrelationOptimization = false;
                    this.brokerEnabled = false;
                    this.parameterization = false;
                    this.isReadCommittedSnapshotOn = false;
                    this.allowSnapshotIsolation = false;
                }

                this.varDecimalEnabled =
                    // db.IsVarDecimalStorageFormatSupported &&
                    db.IsVarDecimalStorageFormatEnabled;

                // SQL Server 2008 options
                // Both EncryptionEnabled and HonorPriority are added in SQL Server 2008 only and hence one condition is enough
                if (db.IsSupportedProperty("EncryptionEnabled"))
                {
                    this.encryptionEnabled = db.EncryptionEnabled;
                    this.honorBrokerPriority = db.HonorBrokerPriority;
                    this.varDecimalEnabled = true;
                }
                else
                {
                    this.encryptionEnabled = false;
                    this.honorBrokerPriority = false;
                }

                if (db.IsSupportedProperty("FilestreamDirectoryName"))
                {
                    this.filestreamDirectoryName = db.FilestreamDirectoryName;
                    this.filestreamNonTransactedAccess = db.FilestreamNonTransactedAccess;
                }

                try
                {
                    if (context.Server.IsSupportedProperty("IsFullTextInstalled") && context.Server.IsFullTextInstalled)
                    {
                        // Full-text indexing will always be enabled in Katmai
                        if (db.IsSupportedProperty("IsFullTextEnabled"))
                        {
                            this.fullTextIndexingEnabled = db.IsFullTextEnabled;
                        }
                        else
                        {
                            this.fullTextIndexingEnabled = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    SqlException sqlException = CUtils.GetSqlException(ex);
                    if (null != sqlException && true == CUtils.IsPermissionDeniedException(sqlException))
                    {
                        // assume false
                        this.fullTextIndexingEnabled = false;
                    }
                    else
                    {
                        throw;
                    }
                }


                if ((db.CompatibilityLevel == CompatibilityLevel.Version60) ||
                    (db.CompatibilityLevel == CompatibilityLevel.Version65))
                {
                    string errorMessage = manager.GetString("error_60compatibility");
                    throw new InvalidOperationException(errorMessage);
                }        

                this.databaseCompatibilityLevel = db.CompatibilityLevel;

                if (db.IsSupportedProperty("ContainmentType"))
                {
                    this.databaseContainmentType = db.ContainmentType;
                    this.defaultFulltextLanguageLcid = db.DefaultFullTextLanguage.Lcid;
                    this.defaultLanguageLcid = db.DefaultLanguage.Lcid;
                    this.nestedTriggersEnabled = db.NestedTriggersEnabled;
                    this.transformNoiseWords = db.TransformNoiseWords;
                    this.twoDigitYearCutoff = db.TwoDigitYearCutoff;
                }

                // SQL Server 2011 options
                // TargetRecoveryTime is added in SQL Server 2011
                if (db.IsSupportedProperty("TargetRecoveryTime"))
                {
                    this.targetRecoveryTime = db.TargetRecoveryTime;
                }

                try
                {
                    if (db.IsSupportedProperty("IsMirroringEnabled") && db.IsMirroringEnabled)
                    {
                        this.mirrorSafetyLevel = db.MirroringSafetyLevel;
                        this.witnessServer = db.MirroringWitness;
                    }
                }
                catch (Exception ex)
                {
                    SqlException sqlException = CUtils.GetSqlException(ex);
                    if (null != sqlException && true == CUtils.IsPermissionDeniedException(sqlException))
                    {
                        /// do nothing
                    }
                    else
                    {
                        throw ex;
                    }
                }

                // SQL Server 2014 options
                // DelayedDurability is added in SQL Server 2014
                if (db.IsSupportedProperty("DelayedDurability"))
                {
                    this.delayedDurability = db.DelayedDurability;
                }

                //Only fill in the Azure properties when connected to an Azure server
                if (context.Server.ServerType == DatabaseEngineType.SqlAzureDatabase)
                {
                    this.azureEditionDisplayValue = db.AzureEdition;
                    AzureEdition edition;
                    if (Enum.TryParse(db.AzureEdition, true, out edition))
                    {
                        this.azureEdition = edition;
                    }
                    else
                    {
                        // Unknown Azure DB Edition so we can't set a value, leave as default Standard
                        // Note that this is likely a 
                    }

                    //Size is in MB, but if it's greater than a GB we want to display the size in GB
                    //We do this to be on par with what the management portal displays
                    if (db.Size >= 1024)
                    {
                        this.maxSize = new DbSize((int)(db.Size / 1024.0), DbSize.SizeUnits.GB);
                    }
                    else
                    {
                        this.maxSize = new DbSize((int)db.Size, DbSize.SizeUnits.MB);
                    }

                    this.GetServiceLevelObjectiveValues(context);

                }

                // Check if we support database scoped configurations on this server. Since these were all added at the same time,
                // only check if MaxDop is supported rather than each individual property.
                if (db.IsSupportedProperty("MaxDop"))
                {
                    this.maxDop = db.MaxDop;
                    this.maxDopForSecondary = db.MaxDopForSecondary;
                    this.legacyCardinalityEstimation = db.LegacyCardinalityEstimation;
                    this.legacyCardinalityEstimationForSecondary = db.LegacyCardinalityEstimationForSecondary;
                    this.parameterSniffing = db.ParameterSniffing;
                    this.parameterSniffingForSecondary = db.ParameterSniffingForSecondary;
                    this.queryOptimizerHotfixes = db.QueryOptimizerHotfixes;
                    this.queryOptimizerHotfixesForSecondary = db.QueryOptimizerHotfixesForSecondary;
                }
            }

            /// <summary>
            /// Fetches the values of the current and configured Service Level Objective
            /// for the target DB of this object
            /// </summary>
            private void GetServiceLevelObjectiveValues(CDataContainer context)
            {
                Database db = context.Server.Databases[this.name];

                //For Azure v12 or later we can use SMO (the property doesn't exist prior to v12)
                if (Utils.IsSql12OrLater(context.Server.Information.Version.Major))
                {
                    //Currently the only way to get the configured service level objective is to use the REST API. 
                    //Since SSMS doesn't currently support that we'll leave it blank for now until support is 
                    //added or T-SQL supports getting the configured SLO
                    this.configuredServiceLevelObjective = "";
                    this.currentServiceLevelObjective = db.AzureServiceObjective;
                }
                else
                { //If it's under v12 we need to query the master DB directly since that has the views containing the necessary information
                    using (var conn = new SqlConnection(context.Server.ConnectionContext.ConnectionString))
                    {
                        using (var cmd = new SqlCommand(dbSloQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@DbID", db.ID);
                            conn.Open();
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    this.configuredServiceLevelObjective = reader["configured_slo_name"].ToString();
                                    this.currentServiceLevelObjective = reader["current_slo_name"].ToString();
                                    break; //Got our service level objective so we're done
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Creates an instance of DatabaseData  - Copy constructor
            /// </summary>
            /// <param name="other"></param>
            public DatabaseData(DatabaseData other)
            {
                this.owner = other.owner;
                this.name = other.name;
                this.collation = other.collation;
                this.recoveryModel = other.recoveryModel;
                this.lastBackupDate = other.lastBackupDate;
                this.lastLogBackupDate = other.lastLogBackupDate;
                this.restrictAccess = other.restrictAccess;
                this.databaseState = other.databaseState;
                this.defaultCursor = other.defaultCursor;
                this.databaseCompatibilityLevel = other.databaseCompatibilityLevel;
                this.pageVerify = other.pageVerify;
                this.closeCursorOnCommit = other.closeCursorOnCommit;
                this.isReadOnly = other.isReadOnly;
                this.autoClose = other.autoClose;
                this.autoShrink = other.autoShrink;
                this.autoCreateStatistics = other.autoCreateStatistics;
                this.autoCreateStatisticsIncremental = other.autoCreateStatisticsIncremental;
                this.autoUpdateStatistics = other.autoUpdateStatistics;
                this.autoUpdateStatisticsAsync = other.autoUpdateStatisticsAsync;
                this.ansiNullDefault = other.ansiNullDefault;
                this.ansiNulls = other.ansiNulls;
                this.ansiWarnings = other.ansiWarnings;
                this.ansiPadding = other.ansiPadding;
                this.arithabort = other.arithabort;
                this.concatNullYieldsNull = other.concatNullYieldsNull;
                this.numericRoundAbort = other.numericRoundAbort;
                this.quotedIdentifier = other.quotedIdentifier;
                this.recursiveTriggers = other.recursiveTriggers;
                this.fullTextIndexingEnabled = other.fullTextIndexingEnabled;
                this.mirrorSafetyLevel = other.mirrorSafetyLevel;
                this.witnessServer = other.witnessServer;
                this.dbChaining = other.dbChaining;
                this.trustworthy = other.trustworthy;
                this.dateCorrelationOptimization = other.dateCorrelationOptimization;
                this.brokerEnabled = other.brokerEnabled;
                this.parameterization = other.parameterization;
                this.varDecimalEnabled = other.varDecimalEnabled;
                this.encryptionEnabled = other.encryptionEnabled;
                this.honorBrokerPriority = other.honorBrokerPriority;
                this.serviceBrokerGuid = other.serviceBrokerGuid;
                this.databaseContainmentType = other.databaseContainmentType;
                this.defaultFulltextLanguageLcid = other.defaultFulltextLanguageLcid;
                this.defaultLanguageLcid = other.defaultLanguageLcid;
                this.nestedTriggersEnabled = other.nestedTriggersEnabled;
                this.transformNoiseWords = other.transformNoiseWords;
                this.twoDigitYearCutoff = other.twoDigitYearCutoff;
                this.isReadCommittedSnapshotOn = other.isReadCommittedSnapshotOn;
                this.allowSnapshotIsolation = other.allowSnapshotIsolation;
                this.filestreamNonTransactedAccess = other.filestreamNonTransactedAccess;
                this.filestreamDirectoryName = other.filestreamDirectoryName;
                this.isSystemDB = other.isSystemDB;
                this.targetRecoveryTime = other.targetRecoveryTime;
                this.delayedDurability = other.delayedDurability;
                this.azureEdition = other.azureEdition;
                this.azureEditionDisplayValue = other.azureEditionDisplayValue;
                this.configuredServiceLevelObjective = other.configuredServiceLevelObjective;
                this.currentServiceLevelObjective = other.currentServiceLevelObjective;
                this.legacyCardinalityEstimation = other.legacyCardinalityEstimation;
                this.legacyCardinalityEstimationForSecondary = other.legacyCardinalityEstimationForSecondary;
                this.maxDop = other.maxDop;
                this.maxDopForSecondary = other.maxDopForSecondary;
                this.parameterSniffing = other.parameterSniffing;
                this.parameterSniffingForSecondary = other.parameterSniffingForSecondary;
                this.queryOptimizerHotfixes = other.queryOptimizerHotfixes;
                this.queryOptimizerHotfixesForSecondary = other.queryOptimizerHotfixesForSecondary;
                this.maxSize = other.maxSize == null ? null : new DbSize(other.maxSize);
            }

            /// <summary>
            /// Clones this instance of DatabaseData
            /// </summary>
            /// <returns></returns>
            public DatabaseData Clone()
            {
                return new DatabaseData(this);
            }

            /// <summary>
            /// Compares 2 instances of DatabaseData
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool HasSameValueAs(DatabaseData other)
            {
                bool result =
                    (this.recoveryModel == other.recoveryModel) &&
                    (this.lastBackupDate == other.lastBackupDate) &&
                    (this.lastLogBackupDate == other.lastLogBackupDate) &&
                    (this.restrictAccess == other.restrictAccess) &&
                    (this.databaseState == other.databaseState) &&
                    (this.defaultCursor == other.defaultCursor) &&
                    (this.databaseCompatibilityLevel == other.databaseCompatibilityLevel) &&
                    (this.pageVerify == other.pageVerify) &&
                    (this.closeCursorOnCommit == other.closeCursorOnCommit) &&
                    (this.isReadOnly == other.isReadOnly) &&
                    (this.autoClose == other.autoClose) &&
                    (this.autoShrink == other.autoShrink) &&
                    (this.autoCreateStatistics == other.autoCreateStatistics) &&
                    (this.autoCreateStatisticsIncremental == other.autoCreateStatisticsIncremental) &&
                    (this.autoUpdateStatistics == other.autoUpdateStatistics) &&
                    (this.autoUpdateStatisticsAsync == other.autoUpdateStatisticsAsync) &&
                    (this.ansiNullDefault == other.ansiNullDefault) &&
                    (this.ansiNulls == other.ansiNulls) &&
                    (this.ansiWarnings == other.ansiWarnings) &&
                    (this.ansiPadding == other.ansiPadding) &&
                    (this.arithabort == other.arithabort) &&
                    (this.concatNullYieldsNull == other.concatNullYieldsNull) &&
                    (this.numericRoundAbort == other.numericRoundAbort) &&
                    (this.quotedIdentifier == other.quotedIdentifier) &&
                    (this.recursiveTriggers == other.recursiveTriggers) &&
                    (this.fullTextIndexingEnabled == other.fullTextIndexingEnabled) &&
                    (this.owner == other.owner) &&
                    (this.collation == other.collation) &&
                    (this.witnessServer == other.witnessServer) &&
                    (this.mirrorSafetyLevel == other.mirrorSafetyLevel) &&
                    (this.dbChaining == other.dbChaining) &&
                    (this.trustworthy == other.trustworthy) &&
                    (this.dateCorrelationOptimization == other.dateCorrelationOptimization) &&
                    (this.brokerEnabled == other.brokerEnabled) &&
                    (this.parameterization == other.parameterization) &&
                    (this.varDecimalEnabled == other.varDecimalEnabled) &&
                    (this.encryptionEnabled == other.encryptionEnabled) &&
                    (this.honorBrokerPriority == other.honorBrokerPriority) &&
                    (this.databaseContainmentType == other.databaseContainmentType) &&
                    (this.defaultFulltextLanguageLcid == other.defaultFulltextLanguageLcid) &&
                    (this.defaultLanguageLcid == other.defaultLanguageLcid) &&
                    (this.nestedTriggersEnabled == other.nestedTriggersEnabled) &&
                    (this.transformNoiseWords == other.transformNoiseWords) &&
                    (this.twoDigitYearCutoff == other.twoDigitYearCutoff) &&
                    (this.targetRecoveryTime == other.targetRecoveryTime) &&
                    (this.isReadCommittedSnapshotOn == other.isReadCommittedSnapshotOn) &&
                    (this.allowSnapshotIsolation == other.allowSnapshotIsolation) &&
                    (this.filestreamNonTransactedAccess == other.filestreamNonTransactedAccess) &&
                    (this.filestreamDirectoryName.Equals(other.filestreamDirectoryName, StringComparison.OrdinalIgnoreCase)) &&
                    (this.allowSnapshotIsolation == other.allowSnapshotIsolation) &&
                    (this.delayedDurability == other.delayedDurability) &&
                    (this.azureEdition == other.azureEdition) &&
                    (this.configuredServiceLevelObjective == other.configuredServiceLevelObjective) &&
                    (this.currentServiceLevelObjective == other.currentServiceLevelObjective) &&
                    (this.maxDop == other.maxDop) &&
                    (this.maxDopForSecondary == other.maxDopForSecondary) &&
                    (this.legacyCardinalityEstimation == other.legacyCardinalityEstimation) &&
                    (this.legacyCardinalityEstimationForSecondary == other.legacyCardinalityEstimationForSecondary) &&
                    (this.parameterSniffing == other.parameterSniffing) &&
                    (this.parameterSniffingForSecondary == other.parameterSniffingForSecondary) &&
                    (this.queryOptimizerHotfixes == other.queryOptimizerHotfixes) &&
                    (this.queryOptimizerHotfixesForSecondary == other.queryOptimizerHotfixesForSecondary) &&
                    (this.queryStoreEnabled == other.queryStoreEnabled) &&
                    (this.maxSize == other.maxSize);

                return result;
            }
        }

        // methods in the based class that modify files and filegroups should work directly against these members
        // methods that use the collections as part of persistence or scripting should work against the corresponding wrapper properties 
        // so derived classes can override behavior
        private readonly List<FilegroupPrototype> filegroups;
        private readonly List<DatabaseFilePrototype> files;

        private readonly List<FilegroupPrototype> removedFilegroups;
        private readonly List<DatabaseFilePrototype> removedFiles;

        protected DatabaseData originalState;
        protected DatabaseData currentState;

        protected CDataContainer context;
        private bool existingDatabase;
        private int numberOfLogFiles;
        protected ServerVersion serverVersion;
        protected Microsoft.SqlServer.Management.Common.DatabaseEngineType databaseEngineType;
        private bool isFilestreamEnabled;

        private event EventHandler observableChanged;
        private bool allowNotifications = true;

        // constants
        private const double kilobytesPerMegabyte = 1024.0d;

        #endregion

        #region properties

        protected DatabaseEngineEdition EditionToCreate { get; set; }

        /// <summary>
        /// Whether or not the UI should show File Groups
        /// </summary>
        public virtual bool HideFileSettings
        {
            get { return false; }
        }

        public virtual bool AllowScripting
        {
            get { return true; }
        }

        /// <summary>
        /// The name of the database
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get
            {
                return this.currentState.name;
            }
            set
            {
                this.currentState.name = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The owner of the database
        /// </summary>
        [Browsable(false)]
        public string Owner
        {
            get
            {
                return this.currentState.owner;
            }
            set
            {
                this.currentState.owner = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The recovery model for the database
        /// </summary>
        [Browsable(false)]
        public virtual RecoveryModel RecoveryModel
        {
            get
            {
                return this.currentState.recoveryModel;
            }
            set
            {
                this.currentState.recoveryModel = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The last backup date for the database
        /// </summary>
        [Browsable(false)]
        public DateTime LastBackupDate
        {
            get
            {
                return this.currentState.lastBackupDate;
            }
            set
            {
                this.currentState.lastBackupDate = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The last log backup date for the database
        /// </summary>
        [Browsable(false)]
        public DateTime LastLogBackupDate
        {
            get
            {
                return this.currentState.lastLogBackupDate;
            }
            set
            {
                this.currentState.lastLogBackupDate = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The collation for the database
        /// </summary>
        [Browsable(false)]
        public string Collation
        {
            get
            {
                return this.currentState.collation;
            }
            set
            {
                this.currentState.collation = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Property return true for System DB
        /// </summary>
        [Browsable(false)]
        public bool IsSystemDB
        {
            get { return this.currentState.isSystemDB; }
        }

        /// <summary>
        /// DatabaseUserAccess for the database
        /// </summary>
        [Category("Category_State"),
        DisplayNameAttribute("Property_RestrictAccess"),
        TypeConverter(typeof(RestrictAccessTypes))]
        public string RestrictAccess
        {
            get
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                string result = null;

                switch (this.currentState.restrictAccess)
                {
                    case DatabaseUserAccess.Multiple:

                        result = manager.GetString("prototype_db_prop_restrictAccess_value_multiple");
                        break;

                    case DatabaseUserAccess.Restricted:

                        result = manager.GetString("prototype_db_prop_restrictAccess_value_restricted");
                        break;

                    case DatabaseUserAccess.Single:

                        result = manager.GetString("prototype_db_prop_restrictAccess_value_single");
                        break;

                }

                return result;
            }
            set
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());

                if (value == manager.GetString("prototype_db_prop_restrictAccess_value_multiple"))
                {
                    this.currentState.restrictAccess = DatabaseUserAccess.Multiple;
                }
                else if (value == manager.GetString("prototype_db_prop_restrictAccess_value_restricted"))
                {
                    this.currentState.restrictAccess = DatabaseUserAccess.Restricted;
                }
                else
                {
                    this.currentState.restrictAccess = DatabaseUserAccess.Single;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Status of the database as text
        /// </summary>
        [Category("Category_State"),
        DisplayNameAttribute("Property_DatabaseState"),
        TypeConverter(typeof(DatabaseStatusTypes))]
        public string DatabaseStateDisplay
        {
            get
            {
                string result = null;
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                
                if ((this.currentState.databaseState & DatabaseStatus.Normal) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_normal"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Restoring) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_restoring"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.RecoveryPending) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_recoveryPending"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Recovering) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_recovering"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Suspect) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_suspect"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Offline) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_offline"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Inaccessible) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_inaccessible"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Standby) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_standby"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.Shutdown) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_shutdown"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.EmergencyMode) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_emergency"));
                }

                if ((this.currentState.databaseState & DatabaseStatus.AutoClosed) != 0)
                {
                    result = this.AppendState(result, manager.GetString("prototype_db_prop_databaseState_value_autoClosed"));
                }

                return result;
            }
        }

        /// <summary>
        /// Status of the database
        /// </summary>
        [Browsable(false)]
        public DatabaseStatus DatabaseState
        {
            get
            {
                return this.currentState.databaseState;
            }
            set
            {
                this.currentState.databaseState = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether cursors should be closed on transaction commit
        /// </summary>
        [Category("Category_Cursor"),
        DisplayNameAttribute("Property_CloseCursorOnCommit")]
        public bool CloseCursorOnCommit
        {
            get
            {
                return this.currentState.closeCursorOnCommit;
            }
            set
            {
                this.currentState.closeCursorOnCommit = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// The default cursor type (local or global)
        /// </summary>
        [Category("Category_Cursor"),
        DisplayNameAttribute("Property_DefaultCursor"),
        TypeConverter(typeof(DefaultCursorTypes))]
        public string DefaultCursorDisplay
        {
            get
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                string result = null;

                switch (this.currentState.defaultCursor)
                {
                    case DefaultCursor.Local:

                        result = manager.GetString("prototype_db_prop_defaultCursor_value_local");
                        break;

                    case DefaultCursor.Global:

                        result = manager.GetString("prototype_db_prop_defaultCursor_value_global");
                        break;
                }

                return result;
            }

            set
            {
                ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
                
                if (value == manager.GetString("prototype_db_prop_defaultCursor_value_local"))
                {
                    this.currentState.defaultCursor = DefaultCursor.Local;
                }
                else
                {
                    this.currentState.defaultCursor = DefaultCursor.Global;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Filestream Non-Transacted Access setting for database.
        /// </summary>
        [Category("Category_Filestream"),
        DisplayNameAttribute("Property_FilestreamNonTransactedAccess")]
        public FilestreamNonTransactedAccessType FilestreamNonTransactedAccess
        {
            get
            {
                return this.currentState.filestreamNonTransactedAccess;
            }
            set
            {
                this.currentState.filestreamNonTransactedAccess = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Filestream share directory name.
        /// </summary>
        [Category("Category_Filestream"),
        DisplayNameAttribute("Property_FilestreamDirectoryName")]
        public string FilestreamDirectoryName
        {
            get
            {
                return this.currentState.filestreamDirectoryName;
            }
            set
            {
                this.currentState.filestreamDirectoryName = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-close
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoClose")]
        public bool AutoClose
        {
            get
            {
                return this.currentState.autoClose;
            }
            set
            {
                this.currentState.autoClose = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-shrink
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoShrink")]
        public bool AutoShrink
        {
            get
            {
                return this.currentState.autoShrink;
            }
            set
            {
                this.currentState.autoShrink = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-create statistics
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoCreateStatistics")]
        public bool AutoCreateStatistics
        {
            get
            {
                return this.currentState.autoCreateStatistics;
            }
            set
            {
                this.currentState.autoCreateStatistics = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-update statistics
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoCreateStatisticsIncremental")]
        public bool AutoCreateStatisticsIncremental
        {
            get
            {
                return this.currentState.autoCreateStatisticsIncremental;
            }
            set
            {
                this.currentState.autoCreateStatisticsIncremental = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Auto-update statistics
        /// </summary>
        [Category("Category_Automatic"),
        DisplayNameAttribute("Property_AutoUpdateStatistics")]
        public bool AutoUpdateStatistics
        {
            get
            {
                return this.currentState.autoUpdateStatistics;
            }
            set
            {
                this.currentState.autoUpdateStatistics = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Use ANSI Null defaults
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_ANSINullsDefault")]
        public bool AnsiNullDefault
        {
            get
            {
                return this.currentState.ansiNullDefault;
            }
            set
            {
                this.currentState.ansiNullDefault = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Use ANSI Nulls
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_ANSINulls")]
        public bool AnsiNulls
        {
            get
            {
                return this.currentState.ansiNulls;
            }
            set
            {
                this.currentState.ansiNulls = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether quoted identifiers are enabled
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_QuotedIdentifier")]
        public bool QuotedIdentifier
        {
            get
            {
                return this.currentState.quotedIdentifier;
            }
            set
            {
                this.currentState.quotedIdentifier = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether recursive triggers are enabled
        /// </summary>
        [Category("Category_Misc"),
        DisplayNameAttribute("Property_RecursiveTriggers")]
        public bool RecursiveTriggers
        {
            get
            {
                return this.currentState.recursiveTriggers;
            }
            set
            {
                this.currentState.recursiveTriggers = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Database compatibility level
        /// </summary>
        [Browsable(false)]
        public CompatibilityLevel DatabaseCompatibilityLevel
        {
            get
            {
                return this.currentState.databaseCompatibilityLevel;
            }
            set
            {
                this.currentState.databaseCompatibilityLevel = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Whether full-text indexing is enabled
        /// </summary>
        [Browsable(false)]
        public bool FullTextIndexing
        {
            get
            {
                return this.currentState.fullTextIndexingEnabled;
            }
            set
            {
                this.currentState.fullTextIndexingEnabled = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// the set of prototype filegroups associated with this prototype database
        /// </summary>
        [Browsable(false)]
        public virtual IList<FilegroupPrototype> Filegroups
        {
            get
            {
                return this.filegroups;
            }
        }

        /// <summary>
        /// The set of prototype files associated with this prototype database
        /// </summary>
        [Browsable(false)]
        public virtual IList<DatabaseFilePrototype> Files
        {
            get
            {
                return this.files;
            }
        }

        /// <summary>
        /// The default filegroup
        /// </summary>
        [Browsable(false)]
        public FilegroupPrototype DefaultFilegroup
        {
            get
            {
                FilegroupPrototype result = null;

                foreach (FilegroupPrototype filegroup in Filegroups)
                {
                    if (filegroup.IsDefault &&
                        !(filegroup.IsFileStream || filegroup.IsMemoryOptimized))
                    {
                        result = filegroup;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The default filestream filegroup
        /// </summary>
        [Browsable(false)]
        public FilegroupPrototype DefaultFileStreamFilegroup
        {
            get
            {
                FilegroupPrototype result = null;

                foreach (FilegroupPrototype filegroup in Filegroups)
                {
                    if (filegroup.IsDefault && filegroup.IsFileStream)
                    {
                        result = filegroup;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// The default memory optimized filegroup
        /// </summary>
        [Browsable(false)]
        public FilegroupPrototype DefaultMemoryOptimizedFilegroup
        {
            get
            {
                FilegroupPrototype result = null;

                foreach (FilegroupPrototype filegroup in Filegroups)
                {
                    if (filegroup.IsDefault && filegroup.IsMemoryOptimized)
                    {
                        result = filegroup;
                        break;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Whether the database exists on the server
        /// </summary>
        [Browsable(false)]
        public bool Exists
        {
            get
            {
                return this.existingDatabase;
            }

            set
            {
                this.existingDatabase = value;
            }
        }

        /// <summary>
        /// The name of the database on the server
        /// </summary>
        [Browsable(false)]
        public string OriginalName
        {
            get
            {
                return this.originalState.name;
            }
        }

        /// <summary>
        /// The number of log files defined for the database
        /// </summary>
        [Browsable(false)]
        public int NumberOfLogFiles
        {
            get
            {
                return this.numberOfLogFiles;
            }
            set
            {
                this.numberOfLogFiles = value;
            }
        }


        /// <summary>
        /// Whether the server version supports per-database collation
        /// </summary>
        [Browsable(false)]
        public bool IsCollationSupported
        {
            get
            {
                // per database collation was a new feature in SQL Server 2000 (version 8)
                return (7 < this.serverVersion.Major);
            }
        }

        // $FUTURE: 6/25/2004-stevetw  Mirroring properties should be moved to
        // a Yukon-specific subclass

        /// <summary>
        /// Mirror Safety level
        /// </summary>
        [Browsable(false)]
        public MirroringSafetyLevel MirrorSafetyLevel
        {
            get
            {
                return this.currentState.mirrorSafetyLevel;
            }
            set
            {
                currentState.mirrorSafetyLevel = value;
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Mirror Witness
        /// </summary>
        [Browsable(false)]
        public string MirrorWitness
        {
            get
            {
                return this.currentState.witnessServer;
            }
            set
            {
                currentState.witnessServer = value;
                this.NotifyObservers();
            }
        }

        // Make sure version checks use this property,
        // not explicit comparisons against the server major version
        /// <summary>
        /// Whether the server is Yukon or later
        /// </summary>
        [Browsable(false)]
        protected bool IsYukonOrLater
        {
            get
            {
                return (9 <= this.serverVersion.Major);
            }
        }

        [Browsable(false)]
        public ServerVersion ServerVersion
        {
            get
            {
                return this.serverVersion;
            }
        }

        [Browsable(false)]
        public Microsoft.SqlServer.Management.Common.DatabaseEngineType DatabaseEngineType
        {
            get
            {
                return this.databaseEngineType;
            }
        }

        /// <summary>
        /// Whether filestream is enabled or not.
        /// </summary>       
        [Browsable(false)]
        public bool IsFilestreamEnabled
        {
            get
            {
                return this.isFilestreamEnabled;
            }
        }


        #endregion

        private StringCollection GetDatabaseDefaultInitFields(Server server)
        {
             StringCollection databaseDefaultInitFields;
             if (context.IsNewObject)
             {
                 databaseDefaultInitFields = server.GetPropertyNames(typeof(Database), server.DatabaseEngineEdition);
             }
             else
             {
                 string databaseName = context.GetDocumentPropertyString("database");
                 databaseDefaultInitFields = server.GetPropertyNames(typeof(Database), this.context.Server.Databases[databaseName].DatabaseEngineEdition);
             }

            //AvailabilityGroupName throws exception for Contained Authentication
            //and at the same time is not required in the Database Properties UI.
            if (databaseDefaultInitFields.Contains("AvailabilityGroupName"))
            {
                databaseDefaultInitFields.Remove("AvailabilityGroupName");
            }

            return databaseDefaultInitFields;
        }

        private StringCollection GetUIDataFileProperties(Server server)
        {
            //VSTS 404074
            //In LPU Scenario, db properties dialog failing with permission exception since it tries to execute DBCC showfilestats command and failing with
            //an exception. In order to improve DB properties dialog performance, a single query gets executed to fetch the information of all properties related to all the files. This resulted in executing DBCC show filestats which an LPU doesnt have privileges to execute.
            //DBCC Showfilestats is executed to fetch UsedSpace and Available Space. These properties are not used anywhere in the dialog.
            //Removing the UsedSpace and AvailableSpace properties from the default init fields list for the data files will able to launch the dialog
            //properly.

            StringCollection dataFileFields = new StringCollection();

            dataFileFields.AddRange(new string[] { "IsPrimaryFile", "GrowthType", "FileName", "Size", "MaxSize", "Name", "ID", "Urn", "Growth" });

            if (Utils.IsYukonOrAbove(server))
            {
                dataFileFields.AddRange(new string[] { "IsReadOnlyMedia", "IsReadOnly", "IsOffline", "IsSparse" });
            }

            return dataFileFields;
        }

        private StringCollection GetUILogFileProperties(Server server)
        {
            StringCollection logFileFields = new StringCollection();

            logFileFields.AddRange(new string[] { "UsedSpace", "GrowthType", "FileName", "Size", "MaxSize", "Name", "ID"
                    , "Urn", "Growth" });

            if (Utils.IsYukonOrAbove(server))
            {
                logFileFields.AddRange(new string[] { "IsReadOnlyMedia", "IsReadOnly", "IsOffline", "IsSparse" });
            }

            return logFileFields;
        }

        /// <summary>
        /// Creates an instance of DatabasePrototype
        /// </summary>
        /// <param name="context"></param>
        public DatabasePrototype(CDataContainer context)
        {
            this.context = context;
            this.serverVersion = context.Server.ConnectionContext.ServerVersion;
            this.databaseEngineType = context.Server.DatabaseEngineType;
            this.isFilestreamEnabled = Utils.FilestreamEnabled(this.context.Server);
            this.files = new List<DatabaseFilePrototype>();
            this.filegroups = new List<FilegroupPrototype>();
            this.removedFilegroups = new List<FilegroupPrototype>();
            this.removedFiles = new List<DatabaseFilePrototype>();
            this.numberOfLogFiles = 0;
            this.EditionToCreate = DatabaseEngineEdition.Unknown;

            StringCollection databaseDefaultInitFields = this.GetDatabaseDefaultInitFields(this.context.Server);
            context.Server.SetDefaultInitFields(typeof(Database), databaseDefaultInitFields);
            context.Server.SetDefaultInitFields(typeof(DataFile), this.GetUIDataFileProperties(this.context.Server));
            context.Server.SetDefaultInitFields(typeof(LogFile), this.GetUILogFileProperties(this.context.Server));
            context.Server.SetDefaultInitFields(typeof(FileGroup), true);

            if (!context.IsNewObject)
            {
                string databaseName = context.GetDocumentPropertyString("database");
                this.LoadDefinition(databaseName);
            }
            else
            {
                try
                {
                    try
                    {
                        //First try to get the properties, if attempt fails get 
                        //only the minimal set of properties by setting the DefaultInitFields value to false
                        this.originalState = new DatabaseData(context, "model");
                    }
                    catch (Exception)
                    {
                        //Now try again with the optimized(DefaultInitFields set to false) properties    
                        context.Server.SetDefaultInitFields(typeof(Database), false);
                        this.originalState = new DatabaseData(context, "model");
                        //Set the DefaultInitFields to its original value
                        context.Server.SetDefaultInitFields(typeof(Database), databaseDefaultInitFields);
                    }
                    this.originalState.owner = String.Empty;
                }
                catch (Exception)
                {
                    //Set the DefaultInitFields to its original value
                    context.Server.SetDefaultInitFields(typeof(Database), databaseDefaultInitFields);
                    this.originalState = new DatabaseData(context);
                }

                // New database should not inherit ReadOnly from model database (Fix TFS 885072)                
                this.originalState.isReadOnly = false;
                this.originalState.name = String.Empty;
                this.currentState = this.originalState.Clone();
                this.existingDatabase = false;
                this.currentState = this.originalState.Clone();                
                //this value should set to false(it is true when it gets here due model db)
                this.originalState.isSystemDB = false;                
            }
        }

        public void Initialize()
        {
            if (!this.Exists && !this.HideFileSettings)
            {
                this.ResetFilegroups();
                this.ResetFiles();
            }
        }

        /// <summary>
        /// Return the prototype database to default values
        /// </summary>
        public void Clear()
        {
            this.currentState = this.originalState.Clone();

            this.ResetFilegroups();
            this.ResetFiles();
        }

        /// <summary>
        /// Set prototype database state to match the state of the existing database
        /// </summary>
        /// <param name="newName">The name of the database whose definition we are loading</param>
        public void LoadDefinition(string newName)
        {
            var sw = new Stopwatch();
            sw.Start();
            this.originalState = new DatabaseData(context, newName);
            sw.Stop();
            Trace.TraceInformation("Time to construct DatabaseData: {0}", sw.ElapsedMilliseconds);
            this.currentState = this.originalState.Clone();
            this.existingDatabase = true;

            this.LoadFilesAndFilegroups();
        }

        /// <summary>
        /// Create or Alter the database on the server
        /// </summary>
        /// <param name="marshallingControl">The control through which UI interactions are to be marshalled</param>
        /// <returns>The SMO database object that was created or modified</returns>
        public virtual Database ApplyChanges()
        {
           Database db = null;

           if (this.ChangesExist())
           {
               bool scripting = (SqlExecutionModes.CaptureSql == this.context.Server.ConnectionContext.SqlExecutionModes);
               bool mustRollback = false;

               db = this.GetDatabase();

               // Other connections will need to be closed if the following is true
               // 1) The database already exists, AND
               // 2) We are not scripting, AND
               //  a) read-only state is changing, OR
               //  b) user-access is changing, OR
               //  c) date correlation optimization is changing

               // There are also additional properties we don't currently expose that also need
               // to be changed when no one else is connected:

               //  d) emergency, OR
               //  e) offline, (moving to offline - obviously not necessary to check when moving from offline)
               //  f) read committed snapshot

               if (this.Exists && !scripting &&
                   ((this.currentState.isReadOnly != this.originalState.isReadOnly) ||
                   (this.currentState.filestreamDirectoryName != this.originalState.filestreamDirectoryName) ||
                   (this.currentState.filestreamNonTransactedAccess != this.originalState.filestreamNonTransactedAccess) ||
                   (this.currentState.restrictAccess != this.originalState.restrictAccess) ||
                   (this.currentState.dateCorrelationOptimization != this.originalState.dateCorrelationOptimization) ||
                   (this.currentState.isReadCommittedSnapshotOn != this.originalState.isReadCommittedSnapshotOn)))
               {

                   // If the user lacks permissions to enumerate other connections (e.g. the user is not SA)
                   // assume there is a connection to close.  This occasionally results in unnecessary
                   // prompts, but the database alter does succeed this way.  If we assume no other connections,
                   // then we get errors when other connections do exist.
                   int numberOfOpenConnections = 1;

                   try
                   {
                       numberOfOpenConnections = db.ActiveConnections;
                   }
                   catch (Exception)
                   {
                       // do nothing - the user doesn't have permission to check whether there are active connections
                       //STrace.LogExCatch(ex);
                   }

                   if (0 < numberOfOpenConnections)
                   {
                    //    DialogResult result = (DialogResult)marshallingControl.Invoke(new SimplePrompt(this.PromptToCloseConnections));
                    //    if (result == DialogResult.No)
                    //    {
                    //        throw new OperationCanceledException();
                    //    }
                        mustRollback = true;
                        throw new OperationCanceledException();                       
                   }
               }

               // create/alter filegroups
               foreach (FilegroupPrototype filegroup in Filegroups)
               {
                   filegroup.ApplyChanges(db);
               }

               // create/alter files
               foreach (DatabaseFilePrototype file in Files)
               {
                   file.ApplyChanges(db);
               }

               // set the database properties
               this.SaveProperties(db);

               // alter the database to match the properties
               if (!this.Exists)
               {
                   // this is to prevent silent creation of db behind users back
                   // eg. the alter statements to set properties fail when filestream directory name is invalid bug #635273 
                   // but create database statement already succeeded


                   // if filestream directory name has been set by user validate it
                   if (!string.IsNullOrEmpty(this.FilestreamDirectoryName))
                   {
                       // check is filestream directory name is valid
                       if (!FileNameHelper.IsValidFilename(this.FilestreamDirectoryName))
                       {
                           string message = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                           SR.Error_InvalidDirectoryName,
                                                           this.FilestreamDirectoryName);
                           throw new ArgumentException(message);
                       }

                       int rowCount = 0;
                       try
                       {

                           //if filestream directory name already exists in this instance
                           string sqlFilestreamQuery = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                                                                    "SELECT * from sys.database_filestream_options WHERE directory_name = {0}",
                                                                     SqlSmoObject.MakeSqlString(this.FilestreamDirectoryName));
                           DataSet filestreamResults = this.context.ServerConnection.ExecuteWithResults(sqlFilestreamQuery);
                           rowCount = filestreamResults.Tables[0].Rows.Count;
                       }
                       catch
                       {
                           // lets not do anything if there is an exception while validating
                           // this is will prevent bugs in validation logic from preventing creation of valid databases
                           // if database settings are invalid create database tsql statement will fail anyways
                       }
                       if (rowCount != 0)
                       {
                           string message = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                           SR.Error_ExistingDirectoryName,
                                                           this.FilestreamDirectoryName, this.Name);
                           throw new ArgumentException(message);

                       }
                   }

                   db.Create();
               }
               else
               {
                   TerminationClause termination =
                       mustRollback ?
                       TerminationClause.RollbackTransactionsImmediately :
                       TerminationClause.FailOnOpenTransactions;

                   db.Alter(termination);
               }

               // have to explicitly set the default filegroup after the database has been created
               foreach (FilegroupPrototype filegroup in Filegroups)
               {
                   if (filegroup.IsDefault && !(filegroup.Exists && db.FileGroups[filegroup.Name].IsDefault))
                   {
                       if ((filegroup.IsFileStream || filegroup.IsMemoryOptimized))
                       {
                           db.SetDefaultFileStreamFileGroup(filegroup.Name);
                       }
                       else
                       {
                           db.SetDefaultFileGroup(filegroup.Name);
                       }
                   }
               }

               FilegroupPrototype fg = null;
               // drop should happen after alter so that if we delete default filegroup it makes another default before deleting.
               // drop removed files and filegroups for existing databases
               if (this.Exists)
               {
                   foreach (FilegroupPrototype filegroup in this.removedFilegroups)
                   {
                       // In case all filegroups are removed from filestream . memory optimized one default will remain and that has to be the last.
                       if ((filegroup.IsFileStream || filegroup.IsMemoryOptimized) &&
                           db.FileGroups[filegroup.Name].IsDefault)
                       {
                           fg = filegroup;
                       }
                       else
                       {
                           filegroup.ApplyChanges(db);
                       }
                   }

                   if (fg != null)
                   {
                       fg.ApplyChanges(db);
                   }

                   foreach (DatabaseFilePrototype file in this.removedFiles)
                   {
                       file.ApplyChanges(db);
                   }
               }

               // SnapshotIsolation and Owner cannot be set during scripting time for a newly creating database
               // and even in capture mode. Hence this check has been made
               if (db.State == SqlSmoState.Existing)
               {
                   if (this.originalState.allowSnapshotIsolation != this.currentState.allowSnapshotIsolation)
                   {
                       db.SetSnapshotIsolation(this.currentState.allowSnapshotIsolation);
                   }

                   // Set the database owner.  Note that setting owner is an "immediate" operation that 
                   // has to happen after the database is created. There is a SMO limitation where SMO 
                   // throws an exception if immediate operations such as SetOwner() are attempted on 
                   // an object that doesn't exist on the server.

                   if ((this.Owner.Length != 0) &&
                       (this.currentState.owner != this.originalState.owner))
                   {
                       //
                       // bug 20000092 says the error message is confusing if this fails, so 
                       // wrap this and throw a nicer error on failure.
                       //
                       try
                       {
                           db.SetOwner(this.Owner, false);
                       }
                       catch (Exception ex)
                       {
                           SqlException sqlException = CUtils.GetSqlException(ex);

                           if ((null != sqlException) && CUtils.IsPermissionDeniedException(sqlException))
                           {
                               
                               throw new Exception(SR.SetOwnerFailed(this.Owner) + ex.ToString());
                           }
                           else
                           {
                               throw;
                           }
                       }
                   }
               }
           }

           return db;
        }

        /// <summary>
        /// Purge Query Store Data.
        /// </summary>
        public void PurgeQueryStoreData()
        {
            Database db = this.GetDatabase();

            // db.QueryStoreOptions.PurgeQueryStoreData();
        }

        /// <summary>
        /// Add a filegroup prototype to the set of filegroup prototypes
        /// </summary>
        /// <param name="filegroup">The filegroup prototype to add</param>
        public void Add(FilegroupPrototype filegroup)
        {
            if ((filegroup != null) && !filegroups.Contains(filegroup))
            {
                // add the filegroup to the set
                if (0 == String.Compare("PRIMARY", filegroup.Name, StringComparison.OrdinalIgnoreCase))
                {
                    filegroups.Insert(0, filegroup);
                }
                else
                {
                    filegroups.Add(filegroup);
                }

                // if the new filegroup is the default filegroup, update the other filegroups
                if (filegroup.IsDefault)
                {
                    SetNewDefaultFileGroup(filegroup);
                }

                // subscribe to default changed events on the filegroup
                filegroup.OnFileGroupDefaultChangedHandler += new FileGroupDefaultChangedEventHandler(OnFileGroupDefaultChanged);
                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Add a file prototype to the set of file prototypes
        /// </summary>
        /// <param name="file">The file prototype to add</param>
        public void Add(DatabaseFilePrototype file)
        {
            if ((file != null) && !this.files.Contains(file))
            {
                if (file.IsPrimaryFile)
                {
                    this.files.Insert(0, file);
                }
                else
                {
                    this.files.Add(file);
                }

                if (FileType.Log == file.DatabaseFileType)
                {
                    ++this.numberOfLogFiles;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Remove a filegroup prototype from the set of filegroup prototypes
        /// </summary>
        /// <param name="filegroup">The filegroup prototype to removed</param>
        public void Remove(FilegroupPrototype filegroup)
        {
            if ((filegroup != null) && filegroups.Contains(filegroup))
            {
                if (!(filegroup.IsFileStream || filegroup.IsMemoryOptimized))
                {
                    if (filegroup.IsDefault)
                    {
                        FilegroupPrototype primary = this.filegroups[0];
                        primary.IsDefault = true;
                    }

                    filegroup.NotifyFileGroupDeleted(this.DefaultFilegroup);
                }
                else
                {
                    if (filegroup.IsDefault)
                    {
                        foreach (FilegroupPrototype fgp in filegroups)
                        {
                            if ((fgp.IsFileStream || fgp.IsMemoryOptimized)
                                && fgp != filegroup)
                            {
                                fgp.IsDefault = true;
                                break;
                            }
                        }
                    }
                    filegroup.NotifyFileGroupDeleted(this.DefaultFileStreamFilegroup);
                }
                filegroups.Remove(filegroup);

                if (filegroup.Exists)
                {
                    this.removedFilegroups.Add(filegroup);
                    filegroup.Removed = true;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Remove a file prototype from the set of file prototypes 
        /// </summary>
        /// <param name="file">The file prototype to remove</param>
        public void Remove(DatabaseFilePrototype file)
        {
            if ((file != null) && files.Contains(file))
            {
                if (file.IsPrimaryFile)
                {
                    throw new InvalidOperationException("unexpected removal of the primary data file");
                }

                if ((1 == this.numberOfLogFiles) && (FileType.Log == file.DatabaseFileType))
                {
                    throw new InvalidOperationException("Unexpected removal of the last log file.");
                }

                files.Remove(file);

                if (file.Exists)
                {
                    this.removedFiles.Add(file);
                    file.Removed = true;
                }

                if (FileType.Log == file.DatabaseFileType)
                {
                    --this.numberOfLogFiles;
                }

                this.NotifyObservers();
            }
        }

        /// <summary>
        /// Reset files and filegroups to match the existing database
        /// </summary>
        private void LoadFilesAndFilegroups()
        {
            this.allowNotifications = false;

            this.files.Clear();
            this.filegroups.Clear();
            this.removedFiles.Clear();
            this.removedFilegroups.Clear();

            //Azure doesn't support files/filegroups so just exit early after clearing the current settings
            if (this.context.Server.ServerType == DatabaseEngineType.SqlAzureDatabase)
            {
                return;
            }

            Database database = context.Server.Databases[this.Name];
            foreach (FileGroup filegroup in database.FileGroups)
            {
                FilegroupPrototype filegroupPrototype = new FilegroupPrototype(this,
                    filegroup.Name,
                    filegroup.ReadOnly,
                    filegroup.IsDefault,
                    filegroup.FileGroupType,
                    true);

                this.Add(filegroupPrototype);

                try
                {
                    foreach (DataFile datafile in filegroup.Files)
                    {
                        DatabaseFilePrototype file = new DatabaseFilePrototype(this, filegroupPrototype, datafile);
                        this.Add(file);
                    }
                }
                catch (ExecutionFailureException)
                {
                    // do nothing

                }
            }

            this.numberOfLogFiles = 0;

            // $ISSUE: SQL_BU_Defect_Tracking-290364-6/25/2004-stevetw
            // Remove the check for snapshots when SMO supports getting 
            // snapshot log file size info.
            bool isSnapshot = false;
            try
            {
                if (database.IsSupportedProperty("IsDatabaseSnapshot"))
                {
                    isSnapshot = this.IsYukonOrLater && database.IsDatabaseSnapshot;
                }
            }
            catch (Exception ex)
            {
                SqlException sqlException = CUtils.GetSqlException(ex);
                if ((null != sqlException) && CUtils.IsPermissionDeniedException(sqlException))
                {
                    // do nothing
                }
                else
                {
                    throw;
                }
            }

            if (!isSnapshot)
            {
                try
                {
                    if (this.context.Server.DatabaseEngineEdition != DatabaseEngineEdition.SqlOnDemand)
                    {
                        foreach (LogFile logfile in database.LogFiles)
                        {
                            DatabaseFilePrototype logfilePrototype = new DatabaseFilePrototype(this, logfile);
                            this.Add(logfilePrototype);
                        }
                    }
                }
                catch (ExecutionFailureException)
                {
                    // do nothing
                }
            }

            this.allowNotifications = true;
            this.NotifyObservers();
        }

        /// <summary>
        /// Commit property changes to the database
        /// </summary>
        /// <param name="db">The database whose properties we are changing</param>
        protected virtual void SaveProperties(Database db)
        {
            if (!this.Exists || (db.DatabaseOptions.UserAccess != this.currentState.restrictAccess))
            {
                db.DatabaseOptions.UserAccess = this.currentState.restrictAccess;
            }

            if (!this.Exists || (db.DatabaseOptions.CloseCursorsOnCommitEnabled != this.CloseCursorOnCommit))
            {
                db.DatabaseOptions.CloseCursorsOnCommitEnabled = this.CloseCursorOnCommit;
            }

            if (db.IsSupportedProperty("LocalCursorsDefault"))
            {
                bool localCursorsDefault = (this.currentState.defaultCursor == DefaultCursor.Local);
                if (!this.Exists || (db.DatabaseOptions.LocalCursorsDefault != localCursorsDefault))
                {
                    db.DatabaseOptions.LocalCursorsDefault = localCursorsDefault;
                }
            }

            if (db.IsSupportedProperty("AutoClose"))
            {
                if (!this.Exists || (db.DatabaseOptions.AutoClose != this.AutoClose))
                {
                    db.DatabaseOptions.AutoClose = this.AutoClose;
                }
            }

            if (!this.Exists || (db.DatabaseOptions.AutoShrink != this.AutoShrink))
            {
                db.DatabaseOptions.AutoShrink = this.AutoShrink;
            }

            if (!this.Exists || (db.DatabaseOptions.AutoCreateStatistics != this.AutoCreateStatistics))
            {
                db.DatabaseOptions.AutoCreateStatistics = this.AutoCreateStatistics;
            }

            if (db.IsSupportedProperty("AutoCreateIncrementalStatisticsEnabled") &&
                (!this.Exists || db.DatabaseOptions.AutoCreateStatisticsIncremental != this.AutoCreateStatisticsIncremental))
            {
                db.DatabaseOptions.AutoCreateStatisticsIncremental = this.AutoCreateStatisticsIncremental;
            }

            if (!this.Exists || (db.DatabaseOptions.AutoUpdateStatistics != this.AutoUpdateStatistics))
            {
                db.DatabaseOptions.AutoUpdateStatistics = this.AutoUpdateStatistics;
            }

            if (!this.Exists || (db.DatabaseOptions.AnsiNullDefault != this.AnsiNullDefault))
            {
                db.DatabaseOptions.AnsiNullDefault = this.AnsiNullDefault;
            }

            if (!this.Exists || (db.DatabaseOptions.AnsiNullsEnabled != this.AnsiNulls))
            {
                db.DatabaseOptions.AnsiNullsEnabled = this.AnsiNulls;
            }

            if (!this.Exists || (db.DatabaseOptions.QuotedIdentifiersEnabled != this.QuotedIdentifier))
            {
                db.DatabaseOptions.QuotedIdentifiersEnabled = this.QuotedIdentifier;
            }

            if (!this.Exists || (db.DatabaseOptions.RecursiveTriggersEnabled != this.RecursiveTriggers))
            {
                db.DatabaseOptions.RecursiveTriggersEnabled = this.RecursiveTriggers;
            }

            if (db.IsSupportedProperty("RecoveryModel"))
            {
                if (!this.Exists || (db.DatabaseOptions.RecoveryModel != this.RecoveryModel))
                {
                    db.DatabaseOptions.RecoveryModel = this.RecoveryModel;
                }
            }

            // user has to be a sysadmin to set full text indexing settings or compatibility level
            // Azure SQL DB doesn't have a fixed server role for sysadmin
            if (db.Parent.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin) || db.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                // Full-text indexing will always be enabled in Katmai
                if (this.serverVersion.Major <= 9 && db.Parent.Information.IsFullTextInstalled &&
                    (!this.Exists || (db.IsFullTextEnabled != this.FullTextIndexing)))
                {
                    db.IsFullTextEnabled = this.FullTextIndexing;
                }

                if (!this.Exists || (db.CompatibilityLevel != this.DatabaseCompatibilityLevel))
                {
                    db.CompatibilityLevel = this.DatabaseCompatibilityLevel;
                }
            }

            // $FUTURE: 6/25/2004-stevetw Consider moving mirroring property sets
            // to a Yukon-specific subclass
            if (db.IsSupportedProperty("IsMirroringEnabled"))
            {
                if (this.Exists && db.IsMirroringEnabled && (db.MirroringSafetyLevel != MirrorSafetyLevel))
                {
                    db.MirroringSafetyLevel = this.MirrorSafetyLevel;
                }

                if (this.Exists && db.IsMirroringEnabled && (string.Compare(db.MirroringWitness, this.MirrorWitness, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    if (this.MirrorWitness.Length == 0) // we want to remove it
                    {
                        db.ChangeMirroringState(MirroringOption.RemoveWitness);
                    }
                    else
                    {
                        db.MirroringWitness = this.MirrorWitness;
                    }
                }
            }

            if (db.IsSupportedProperty("FilestreamDirectoryName"))
            {
                if ((!this.Exists && !string.IsNullOrEmpty(this.FilestreamDirectoryName)) ||
                  (this.Exists && string.Compare(db.FilestreamDirectoryName, this.FilestreamDirectoryName, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    db.FilestreamDirectoryName = this.FilestreamDirectoryName;
                }

                if ((!this.Exists && this.FilestreamNonTransactedAccess != FilestreamNonTransactedAccessType.Off) ||
                    (this.Exists && db.FilestreamNonTransactedAccess != this.FilestreamNonTransactedAccess))
                {
                    db.FilestreamNonTransactedAccess = this.FilestreamNonTransactedAccess;
                }
            }
        }

        /// <summary>
        /// Will calling ApplyChanges do anything?
        /// </summary>
        /// <returns>True if there are changes to apply, false otherwise</returns>
        public bool ChangesExist()
        {
            bool result =
            !this.Exists ||
            this.FileChangesExist() ||
            this.FileGroupChangesExist() ||
            !this.originalState.HasSameValueAs(this.currentState);

            return result;
        }

        /// <summary>
        /// Are there any changes associated with filegroups?
        /// </summary>
        /// <returns>True if there are filegroup changes, false otherwise</returns>
        private bool FileGroupChangesExist()
        {
            bool result = false;

            if (this.removedFilegroups.Count != 0)
            {
                result = true;
            }
            else
            {
                foreach (FilegroupPrototype fgp in this.filegroups)
                {
                    if (fgp.ChangesExist())
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Are there any changes associated with files
        /// </summary>
        /// <returns>True if there are file changes, false otherwise</returns>
        private bool FileChangesExist()
        {
            bool result = false;

            if (this.removedFiles.Count != 0)
            {
                result = true;
            }
            else
            {
                foreach (DatabaseFilePrototype fp in this.files)
                {
                    if (fp.ChangesExist())
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Release current filegroups and create a new default filegroup prototype
        /// </summary>
        private void ResetFilegroups()
        {
            this.allowNotifications = false;

            // unhook events for all the old filegroups
            foreach (FilegroupPrototype oldFilegroup in this.filegroups)
            {
                oldFilegroup.OnFileGroupDefaultChangedHandler -= new FileGroupDefaultChangedEventHandler(OnFileGroupDefaultChanged);
            }

            // create default filegroup
            string defaultFilegroupName = "PRIMARY";
            bool isDefault = true;
            bool isReadOnly = false;
            FilegroupPrototype filegroup = new FilegroupPrototype(this, defaultFilegroupName, isReadOnly, isDefault, FileGroupType.RowsFileGroup, false);

            filegroup.OnFileGroupDefaultChangedHandler += new FileGroupDefaultChangedEventHandler(OnFileGroupDefaultChanged);

            // set filegroup collection to contain default filegroup
            filegroups.Clear();
            this.Add(filegroup);

            this.allowNotifications = true;
            this.NotifyObservers();
        }

        /// <summary>
        /// Release current files and create new default prototype files
        /// </summary>
        private void ResetFiles()
        {
            this.allowNotifications = false;

            // create prototype data and log files
            DatabaseFilePrototype dataPrototype = new DatabaseFilePrototype(context, this, FileType.Data);
            DatabaseFilePrototype logPrototype = new DatabaseFilePrototype(context, this, FileType.Log, "_log");

            dataPrototype.IsPrimaryFile = true;

            // add prototype files to the set of files
            files.Clear();
            files.Add(dataPrototype);
            files.Add(logPrototype);

            this.numberOfLogFiles = 1;

            this.allowNotifications = true;
            this.NotifyObservers();
        }

        /// <summary>
        /// Event handler for changes to file group default-ness
        /// </summary>
        /// <param name="sender">The object that changed</param>
        /// <param name="e">EventArgs describing the change</param>
        private void OnFileGroupDefaultChanged(object sender, BooleanValueChangedEventArgs e)
        {
            // if the default-ness has changed from non-default to default,
            // iterate through the set of filegroups changing all the filegroups
            // that are not the sender to non-default.
            if (e.NewValue)
            {
                FilegroupPrototype newDefault = sender as FilegroupPrototype;

                SetNewDefaultFileGroup(newDefault);
            }
        }

        /// <summary>
        /// Makes all the filegroups that a not the new default filegroup not default
        /// </summary>
        /// <param name="newDefault">The new default filegroup</param>
        private void SetNewDefaultFileGroup(FilegroupPrototype newDefault)
        {
            if (newDefault != null)
            {
                foreach (FilegroupPrototype prototype in this.filegroups)
                {
                    //Make isDefault property of all the other filegroups of the same filegrouptype to false.
                    if ((prototype != newDefault) && prototype.IsDefault &&
                        prototype.FileGroupType == newDefault.FileGroupType)
                    {
                        prototype.IsDefault = false;
                    }
                }
            }
        }

        /// <summary>
        /// Appends a state flag to a string reprenting a combination of flags
        /// </summary>
        /// <param name="fullState">The flag combination, such as "OFFLINE | RECOVERING"</param>
        /// <param name="stateFlag">The flag to append, such as "AUTO_CLOSED"</param>
        /// <returns>The new combination of flags, such as "OFFLINE | RECOVERING | AUTO_CLOSED"</returns>
        private string AppendState(string fullState, string stateFlag)
        {
            string result = null;

            if (fullState == null)
            {
                result = stateFlag;
            }
            else
            {
                result = String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0} | {1}",
                    fullState,
                    stateFlag);
            }

            return result;
        }

        protected Database GetDatabase()
        {
            Database result = null;

            // if we think we exist, get the SMO database object
            if (this.Exists)
            {
                result = this.context.Server.Databases[this.originalState.name];
                if (result == null)
                {
                    throw new Exception("Object does not exist");
                }
            }
            else
            {
                result = new Database(this.context.Server, this.Name, this.EditionToCreate);                
            }
            
            return result;
        }

        /// <summary>
        /// Property to access the observable event.
        /// </summary>
        internal event EventHandler Changed
        {
            add { this.observableChanged += value; }
            remove { this.observableChanged -= value; }
        }

        /// <summary>
        /// Notify all observers that this object has changed.
        /// </summary>
        /// <param name="sender">The object that changed</param>
        /// <param name="e">Hint for the notification, usually null</param>
        internal void NotifyObservers(object sender, EventArgs e)
        {
            if (this.allowNotifications && (this.observableChanged != null))
            {
                this.observableChanged(sender, e);
            }
        }

        /// <summary>
        /// Notify all observers that this object has changed.
        /// </summary>
        internal void NotifyObservers()
        {
            this.NotifyObservers(this, new EventArgs());
        }

        #region IDynamicValues Members

        public virtual TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {

            TypeConverter.StandardValuesCollection result = null;
            ResourceManager manager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
            List<string> standardValues = new List<string>();

            if (context.PropertyDescriptor.Name == "DefaultCursorDisplay")
            {
                standardValues.Add(manager.GetString("prototype_db_prop_defaultCursor_value_local"));
                standardValues.Add(manager.GetString("prototype_db_prop_defaultCursor_value_global"));
            }
            else if (context.PropertyDescriptor.Name == "RestrictAccess")
            {
                standardValues.Add(manager.GetString("prototype_db_prop_restrictAccess_value_multiple"));
                standardValues.Add(manager.GetString("prototype_db_prop_restrictAccess_value_single"));
                standardValues.Add(manager.GetString("prototype_db_prop_restrictAccess_value_restricted"));
            }

            if (standardValues.Count > 0)
            {
                result = new TypeConverter.StandardValuesCollection(standardValues);
            }

            return result;
        }

        #endregion
    }
}
