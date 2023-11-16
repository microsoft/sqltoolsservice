//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using static Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Text;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;
using System.Collections.Specialized;
using Microsoft.SqlTools.SqlCore.Utility;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>   
    /// Database object type handler
    /// </summary>
    public class DatabaseHandler : ObjectTypeHandler<DatabaseInfo, DatabaseViewContext>
    {
        private const int minimumVersionForWritableCollation = 8;
        private const int minimumVersionForRecoveryModel = 8;
        private const string serverNotExistsError = "Server was not created for data container";

        private static readonly Dictionary<CompatibilityLevel, string> displayCompatLevels = new Dictionary<CompatibilityLevel, string>();
        private static readonly Dictionary<ContainmentType, string> displayContainmentTypes = new Dictionary<ContainmentType, string>();
        private static readonly Dictionary<RecoveryModel, string> displayRecoveryModels = new Dictionary<RecoveryModel, string>();
        private static readonly Dictionary<PageVerify, string> displayPageVerifyOptions = new Dictionary<PageVerify, string>();
        private static readonly Dictionary<DatabaseUserAccess, string> displayRestrictAccessOptions = new Dictionary<DatabaseUserAccess, string>();
        private static readonly ConcurrentDictionary<FileType, string> displayFileTypes = new ConcurrentDictionary<FileType, string>();
        private static readonly ConcurrentDictionary<QueryStoreOperationMode, string> displayOperationModeOptions = new ConcurrentDictionary<QueryStoreOperationMode, string>();
        private static readonly ConcurrentDictionary<QueryStoreCaptureMode, string> displayQueryStoreCaptureModeOptions = new ConcurrentDictionary<QueryStoreCaptureMode, string>();
        private static readonly SortedDictionary<int, string> displayStatisticsCollectionIntervalInMinutes = new SortedDictionary<int, string>();
        private static readonly SortedDictionary<int, string> displayQueryStoreStaleThresholdInHours = new SortedDictionary<int, string>();
        private static readonly ConcurrentDictionary<QueryStoreSizeBasedCleanupMode, string> displaySizeBasedCleanupMode = new ConcurrentDictionary<QueryStoreSizeBasedCleanupMode, string>();

        private static readonly Dictionary<string, CompatibilityLevel> compatLevelEnums = new Dictionary<string, CompatibilityLevel>();
        private static readonly Dictionary<string, ContainmentType> containmentTypeEnums = new Dictionary<string, ContainmentType>();
        private static readonly Dictionary<string, RecoveryModel> recoveryModelEnums = new Dictionary<string, RecoveryModel>();
        private static readonly Dictionary<string, FileType> fileTypesEnums = new Dictionary<string, FileType>();
        private static readonly Dictionary<string, QueryStoreOperationMode> operationModeEnums = new Dictionary<string, QueryStoreOperationMode>();
        private static readonly Dictionary<string, QueryStoreCaptureMode> captureModeEnums = new Dictionary<string, QueryStoreCaptureMode>();
        private static readonly Dictionary<string, int> statisticsCollectionIntervalValues = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> queryStoreStaleThresholdValues = new Dictionary<string, int>();

        internal static readonly string[] AzureEditionNames;
        internal static readonly string[] AzureBackupLevels;
        internal static readonly string[] PropertiesOnOffOptions;
        internal static readonly string[] DscElevateOptions;
        internal static readonly string[] DscEnableDisableOptions;
        internal static readonly CategoryValue[] displayRecoveryStateOptions;
        internal static readonly AzureEditionDetails[] AzureMaxSizes;
        internal static readonly AzureEditionDetails[] AzureServiceLevels;
        internal DatabaseScopedConfigurationCollection? databaseScopedConfigurationsCollection = null;

        static DatabaseHandler()
        {
            displayCompatLevels.Add(CompatibilityLevel.Version70, SR.compatibilityLevel_sphinx);
            displayCompatLevels.Add(CompatibilityLevel.Version80, SR.compatibilityLevel_shiloh);
            displayCompatLevels.Add(CompatibilityLevel.Version90, SR.compatibilityLevel_yukon);
            displayCompatLevels.Add(CompatibilityLevel.Version100, SR.compatibilityLevel_katmai);
            displayCompatLevels.Add(CompatibilityLevel.Version110, SR.compatibilityLevel_denali);
            displayCompatLevels.Add(CompatibilityLevel.Version120, SR.compatibilityLevel_sql14);
            displayCompatLevels.Add(CompatibilityLevel.Version130, SR.compatibilityLevel_sql15);
            displayCompatLevels.Add(CompatibilityLevel.Version140, SR.compatibilityLevel_sql2017);
            displayCompatLevels.Add(CompatibilityLevel.Version150, SR.compatibilityLevel_sqlv150);
            displayCompatLevels.Add(CompatibilityLevel.Version160, SR.compatibilityLevel_sqlv160);

            displayContainmentTypes.Add(ContainmentType.None, SR.general_containmentType_None);
            displayContainmentTypes.Add(ContainmentType.Partial, SR.general_containmentType_Partial);

            displayRecoveryModels.Add(RecoveryModel.Full, SR.general_recoveryModel_full);
            displayRecoveryModels.Add(RecoveryModel.BulkLogged, SR.general_recoveryModel_bulkLogged);
            displayRecoveryModels.Add(RecoveryModel.Simple, SR.general_recoveryModel_simple);

            displayPageVerifyOptions.Add(PageVerify.Checksum, SR.prototype_db_prop_pageVerify_value_checksum);
            displayPageVerifyOptions.Add(PageVerify.TornPageDetection, SR.prototype_db_prop_pageVerify_value_tornPageDetection);
            displayPageVerifyOptions.Add(PageVerify.None, SR.prototype_db_prop_pageVerify_value_none);

            displayRestrictAccessOptions.Add(DatabaseUserAccess.Multiple, SR.prototype_db_prop_restrictAccess_value_multiple);
            displayRestrictAccessOptions.Add(DatabaseUserAccess.Single, SR.prototype_db_prop_restrictAccess_value_single);
            displayRestrictAccessOptions.Add(DatabaseUserAccess.Restricted, SR.prototype_db_prop_restrictAccess_value_restricted);

            displayFileTypes.TryAdd(FileType.Data, SR.prototype_file_dataFile);
            displayFileTypes.TryAdd(FileType.Log, SR.prototype_file_logFile);
            displayFileTypes.TryAdd(FileType.FileStream, SR.prototype_file_filestreamFile);

            displayOperationModeOptions.TryAdd(QueryStoreOperationMode.Off, CommonConstants.QueryStoreOperationMode_Off);
            displayOperationModeOptions.TryAdd(QueryStoreOperationMode.ReadOnly, CommonConstants.QueryStoreOperationMode_ReadOnly);
            displayOperationModeOptions.TryAdd(QueryStoreOperationMode.ReadWrite, CommonConstants.QueryStoreOperationMode_ReadWrite);

            displayQueryStoreCaptureModeOptions.TryAdd(QueryStoreCaptureMode.All, SR.querystorecapturemode_all);
            displayQueryStoreCaptureModeOptions.TryAdd(QueryStoreCaptureMode.Auto, SR.querystorecapturemode_auto);
            displayQueryStoreCaptureModeOptions.TryAdd(QueryStoreCaptureMode.None, SR.querystorecapturemode_none);

            displayStatisticsCollectionIntervalInMinutes.TryAdd(1, SR.statisticsCollectionInterval_OneMinute);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(5, SR.statisticsCollectionInterval_FiveMinutes);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(10, SR.statisticsCollectionInterval_TenMinutes);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(15, SR.statisticsCollectionInterval_FifteenMinutes);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(30, SR.statisticsCollectionInterval_ThirtyMinutes);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(60, SR.statisticsCollectionInterval_OneHour);
            displayStatisticsCollectionIntervalInMinutes.TryAdd(1440, SR.statisticsCollectionInterval_OneDay);

            displayQueryStoreStaleThresholdInHours.TryAdd(1, SR.queryStore_stale_threshold_OneHour);
            displayQueryStoreStaleThresholdInHours.TryAdd(4, SR.queryStore_stale_threshold_FourHours);
            displayQueryStoreStaleThresholdInHours.TryAdd(8, SR.queryStore_stale_threshold_EightHours);
            displayQueryStoreStaleThresholdInHours.TryAdd(12, SR.queryStore_stale_threshold_TwelveHours);
            displayQueryStoreStaleThresholdInHours.TryAdd(24, SR.queryStore_stale_threshold_OneDay);
            displayQueryStoreStaleThresholdInHours.TryAdd(72, SR.queryStore_stale_threshold_ThreeDays);
            displayQueryStoreStaleThresholdInHours.TryAdd(168, SR.queryStore_stale_threshold_SevenDays);

            displaySizeBasedCleanupMode.TryAdd(QueryStoreSizeBasedCleanupMode.Off, SR.queryStoreSizeBasedCleanupMode_Off);
            displaySizeBasedCleanupMode.TryAdd(QueryStoreSizeBasedCleanupMode.Auto, SR.queryStoreSizeBasedCleanupMode_Auto);
            displayRecoveryStateOptions = new CategoryValue[]{
                new CategoryValue
                {
                    Name = "WithRecovery",
                    DisplayName = "RESTORE WITH RECOVERY"
                },
                new CategoryValue
                {
                    Name = "WithNoRecovery",
                    DisplayName = "RESTORE WITH NORECOVERY"
                },
                new CategoryValue
                {
                    Name = "WithStandBy",
                    DisplayName = "RESTORE WITH STANDBY"
                }
            };

            PropertiesOnOffOptions = new[]{
                CommonConstants.PropertiesDropdown_Value_On,
                CommonConstants.PropertiesDropdown_Value_Off
            };

            DscElevateOptions = new[]{
                CommonConstants.PropertiesDropdown_Value_Off,
                CommonConstants.DatabaseScopedConfigurations_Value_When_supported,
                CommonConstants.DatabaseScopedConfigurations_Value_Fail_Unsupported
            };

            DscEnableDisableOptions = new[]{
                CommonConstants.DatabaseScopedConfigurations_Value_Enabled,
                CommonConstants.DatabaseScopedConfigurations_Value_Disabled
            };

            // Set up maps from displayName to enum type so we can retrieve the equivalent enum types later when getting a Save/Script request.
            // We can't use a simple Enum.Parse for that since the displayNames get localized.
            foreach (CompatibilityLevel key in displayCompatLevels.Keys)
            {
                compatLevelEnums.Add(displayCompatLevels[key], key);
            }
            foreach (ContainmentType key in displayContainmentTypes.Keys)
            {
                containmentTypeEnums.Add(displayContainmentTypes[key], key);
            }
            foreach (RecoveryModel key in displayRecoveryModels.Keys)
            {
                recoveryModelEnums.Add(displayRecoveryModels[key], key);
            }
            foreach (FileType key in displayFileTypes.Keys)
            {
                fileTypesEnums.Add(displayFileTypes[key], key);
            }
            foreach (QueryStoreOperationMode key in displayOperationModeOptions.Keys)
            {
                operationModeEnums.Add(displayOperationModeOptions[key], key);
            }
            foreach (QueryStoreCaptureMode key in displayQueryStoreCaptureModeOptions.Keys)
            {
                captureModeEnums.Add(displayQueryStoreCaptureModeOptions[key], key);
            }
            foreach (KeyValuePair<int, string> pair in displayStatisticsCollectionIntervalInMinutes)
            {
                statisticsCollectionIntervalValues.Add(pair.Value, pair.Key);
            }
            foreach (KeyValuePair<int, string> pair in displayQueryStoreStaleThresholdInHours)
            {
                queryStoreStaleThresholdValues.Add(pair.Value, pair.Key);
            }

            // Azure SLO info is invariant of server information, so set up static objects we can return later
            var editions = AzureSqlDbHelper.GetValidAzureEditionOptions();
            AzureEditionNames = editions.Select(edition => edition.DisplayName).ToArray();
            AzureBackupLevels = AzureSqlDbHelper.BackupStorageRedundancyLevels;
            AzureMaxSizes = GetAzureMaxSizes(editions);
            AzureServiceLevels = GetAzureServiceLevels(editions);
        }

        public DatabaseHandler(ConnectionService connectionService) : base(connectionService)
        {
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Database;
        }

        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams requestParams)
        {
            // create a default data context and database object
            using (var dataContainer = CreateDatabaseDataContainer(requestParams.ConnectionUri, requestParams.IsNewObject, requestParams.Database))
            {
                if (dataContainer.Server == null)
                {
                    throw new InvalidOperationException(serverNotExistsError);
                }

                using (var taskHelper = new DatabaseTaskHelper(dataContainer))
                {
                    var prototype = taskHelper.Prototype;
                    var azurePrototype = prototype as DatabasePrototypeAzure;
                    bool isDw = azurePrototype != null && azurePrototype.AzureEdition == AzureEdition.DataWarehouse;
                    bool isAzureDB = dataContainer.Server.ServerType == DatabaseEngineType.SqlAzureDatabase;
                    bool isManagedInstance = dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;
                    bool isSqlOnDemand = dataContainer.Server.Information.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand;

                    var databaseViewInfo = new DatabaseViewInfo()
                    {
                        ObjectInfo = new DatabaseInfo(),
                        IsAzureDB = isAzureDB,
                        IsManagedInstance = isManagedInstance,
                        IsSqlOnDemand = isSqlOnDemand
                    };

                    // Collect the Database properties information
                    if (!requestParams.IsNewObject)
                    {
                        var smoDatabase = dataContainer.SqlDialogSubject as Database;
                        if (smoDatabase != null)
                        {
                            databaseViewInfo.ObjectInfo = new DatabaseInfo()
                            {
                                Name = smoDatabase.Name,
                                CollationName = smoDatabase.Collation,
                                CompatibilityLevel = displayCompatLevels[smoDatabase.CompatibilityLevel],
                                DateCreated = smoDatabase.CreateDate.ToString(),
                                MemoryAllocatedToMemoryOptimizedObjectsInMb = ByteConverter.ConvertKbtoMb(smoDatabase.MemoryAllocatedToMemoryOptimizedObjectsInKB),
                                MemoryUsedByMemoryOptimizedObjectsInMb = ByteConverter.ConvertKbtoMb(smoDatabase.MemoryUsedByMemoryOptimizedObjectsInKB),
                                NumberOfUsers = smoDatabase.Users.Count,
                                Owner = smoDatabase.Owner,
                                SizeInMb = smoDatabase.Size,
                                SpaceAvailableInMb = ByteConverter.ConvertKbtoMb(smoDatabase.SpaceAvailable),
                                Status = smoDatabase.Status.ToString(),
                                AutoCreateIncrementalStatistics = smoDatabase.AutoCreateIncrementalStatisticsEnabled,
                                AutoCreateStatistics = smoDatabase.AutoCreateStatisticsEnabled,
                                AutoShrink = smoDatabase.AutoShrink,
                                AutoUpdateStatistics = smoDatabase.AutoUpdateStatisticsEnabled,
                                AutoUpdateStatisticsAsynchronously = smoDatabase.AutoUpdateStatisticsAsync,
                                EncryptionEnabled = smoDatabase.EncryptionEnabled,
                                DatabaseScopedConfigurations = smoDatabase.IsSupportedObject<DatabaseScopedConfiguration>() ? GetDSCMetaData(smoDatabase.DatabaseScopedConfigurations) : null,
                                BackupEncryptors = BackupOperation.GetBackupEncryptors(dataContainer.Server!).ToArray()
                            };

                            if (!isAzureDB)
                            {
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).ContainmentType = displayContainmentTypes[smoDatabase.ContainmentType];
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).RecoveryModel = displayRecoveryModels[smoDatabase.RecoveryModel];
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).LastDatabaseBackup = smoDatabase.LastBackupDate == DateTime.MinValue ? SR.databaseBackupDate_None : smoDatabase.LastBackupDate.ToString();
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).LastDatabaseLogBackup = smoDatabase.LastLogBackupDate == DateTime.MinValue ? SR.databaseBackupDate_None : smoDatabase.LastLogBackupDate.ToString();
                                if (prototype is DatabasePrototype130)
                                {
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).QueryStoreOptions = new QueryStoreOptions()
                                    {
                                        ActualMode = displayOperationModeOptions[smoDatabase.QueryStoreOptions.ActualState],
                                        DataFlushIntervalInMinutes = smoDatabase.QueryStoreOptions.DataFlushIntervalInSeconds / 60,
                                        StatisticsCollectionInterval = displayStatisticsCollectionIntervalInMinutes[(int)smoDatabase.QueryStoreOptions.StatisticsCollectionIntervalInMinutes],
                                        MaxPlansPerQuery = smoDatabase.QueryStoreOptions.MaxPlansPerQuery,
                                        MaxSizeInMB = smoDatabase.QueryStoreOptions.MaxStorageSizeInMB,
                                        QueryStoreCaptureMode = smoDatabase.QueryStoreOptions.QueryCaptureMode.ToString(),
                                        SizeBasedCleanupMode = smoDatabase.QueryStoreOptions.SizeBasedCleanupMode.ToString(),
                                        StaleQueryThresholdInDays = smoDatabase.QueryStoreOptions.StaleQueryThresholdInDays,
                                        CurrentStorageSizeInMB = smoDatabase.QueryStoreOptions.CurrentStorageSizeInMB
                                    };
                                    if (prototype is DatabasePrototype140)
                                    {
                                        ((DatabaseInfo)databaseViewInfo.ObjectInfo).QueryStoreOptions!.WaitStatisticsCaptureMode = smoDatabase.QueryStoreOptions.WaitStatsCaptureMode.ToString();
                                    };
                                    if (prototype is DatabasePrototype150)
                                    {
                                        ((DatabaseInfo)databaseViewInfo.ObjectInfo).QueryStoreOptions!.CapturePolicyOptions = new QueryStoreCapturePolicyOptions()
                                        {
                                            ExecutionCount = smoDatabase.QueryStoreOptions.CapturePolicyExecutionCount,
                                            StaleThreshold = displayQueryStoreStaleThresholdInHours[(int)smoDatabase.QueryStoreOptions.CapturePolicyStaleThresholdInHrs],
                                            TotalCompileCPUTimeInMS = smoDatabase.QueryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS,
                                            TotalExecutionCPUTimeInMS = smoDatabase.QueryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS
                                        };

                                        // Sql Server 2019 and higher only support the custom query store capture mode
                                        displayQueryStoreCaptureModeOptions.TryAdd(QueryStoreCaptureMode.Custom, SR.querystorecapturemode_custom);
                                    }
                                }
                            }
                            if (!isManagedInstance)
                            {
                                databaseViewInfo.PageVerifyOptions = displayPageVerifyOptions.Values.ToArray();
                                databaseViewInfo.RestrictAccessOptions = displayRestrictAccessOptions.Values.ToArray();
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).DatabaseReadOnly = smoDatabase.ReadOnly;
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).RestrictAccess = displayRestrictAccessOptions[smoDatabase.UserAccess];
                                if (!isAzureDB)
                                {
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).PageVerify = displayPageVerifyOptions[smoDatabase.PageVerify];
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).TargetRecoveryTimeInSec = smoDatabase.TargetRecoveryTime;
                                    // Files tab is only supported in SQL Server, but files exists for all servers and used in detach database, cannot depend on files property to check the supportability
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).IsFilesTabSupported = true;
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).Filegroups = GetFileGroups(smoDatabase, databaseViewInfo);
                                    try
                                    {
                                        // ServerFilestreamAccessLevel uses xp_instance_regread so will throw if that isn't available. In that case we just default to Disabled to ensure the UI reflects that
                                        databaseViewInfo.ServerFilestreamAccessLevel = dataContainer.Server.FilestreamLevel;
                                    }
                                    catch (PropertyCannotBeRetrievedException ex)
                                    {
                                        // Cannot retrieve FilestreamLevel, defaulting to disabled level
                                        databaseViewInfo.ServerFilestreamAccessLevel = FileStreamEffectiveLevel.Disabled;
                                        Logger.Error($"Cannot retrieve FilestreamLevel, defaulting to disabled level. Error: '{ex.Message}'");
                                    }
                                }

                                if (prototype is DatabasePrototype160)
                                {
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).IsLedgerDatabase = smoDatabase.IsLedger;
                                }
                            }
                            databaseScopedConfigurationsCollection = smoDatabase.IsSupportedObject<DatabaseScopedConfiguration>() ? smoDatabase.DatabaseScopedConfigurations : null;
                            databaseViewInfo.FileTypesOptions = displayFileTypes.Values.ToArray();
                        }
                        databaseViewInfo.PropertiesOnOffOptions = PropertiesOnOffOptions;
                        databaseViewInfo.DscElevateOptions = DscElevateOptions;
                        databaseViewInfo.DscEnableDisableOptions = DscEnableDisableOptions;
                        databaseViewInfo.OperationModeOptions = displayOperationModeOptions.Values.ToArray();
                        databaseViewInfo.QueryStoreCaptureModeOptions = displayQueryStoreCaptureModeOptions.Values.ToArray();
                        databaseViewInfo.StatisticsCollectionIntervalOptions = displayStatisticsCollectionIntervalInMinutes.Values.ToArray();
                        databaseViewInfo.StaleThresholdOptions = displayQueryStoreStaleThresholdInHours.Values.ToArray();
                        databaseViewInfo.SizeBasedCleanupModeOptions = displaySizeBasedCleanupMode.Values.ToArray();
                    }

                    // azure sql db doesn't have a sysadmin fixed role
                    var compatibilityLevelEnabled = !isDw && (dataContainer.LoggedInUserIsSysadmin || isAzureDB);
                    if (isAzureDB)
                    {
                        // Azure doesn't allow modifying the collation after DB creation
                        bool collationEnabled = !prototype.Exists;
                        if (isDw)
                        {
                            if (collationEnabled)
                            {
                                databaseViewInfo.CollationNames = GetCollationsWithPrototypeCollation(prototype);
                            }
                            databaseViewInfo.CompatibilityLevels = GetCompatibilityLevelsAzure(prototype);
                        }
                        else
                        {
                            if (collationEnabled)
                            {
                                databaseViewInfo.CollationNames = GetCollations(dataContainer.Server, prototype, dataContainer.IsNewObject);
                            }
                            if (compatibilityLevelEnabled)
                            {
                                databaseViewInfo.CompatibilityLevels = GetCompatibilityLevels(dataContainer.SqlServerVersion, prototype);
                            }
                        }
                        databaseViewInfo.AzureBackupRedundancyLevels = AzureBackupLevels;
                        databaseViewInfo.AzureServiceLevelObjectives = AzureServiceLevels;
                        databaseViewInfo.AzureEditions = AzureEditionNames;
                        databaseViewInfo.AzureMaxSizes = AzureMaxSizes;
                    }
                    else
                    {
                        databaseViewInfo.CollationNames = GetCollations(dataContainer.Server, prototype, dataContainer.IsNewObject);
                        if (compatibilityLevelEnabled)
                        {
                            databaseViewInfo.CompatibilityLevels = GetCompatibilityLevels(dataContainer.SqlServerVersion, prototype);
                        }

                        // These aren't included when the target DB is on Azure so only populate if it's not an Azure DB
                        databaseViewInfo.RecoveryModels = GetRecoveryModels(dataContainer.Server, prototype);
                        databaseViewInfo.ContainmentTypes = GetContainmentTypes(dataContainer.Server, prototype);
                        if (!requestParams.IsNewObject)
                        {
                            var smoDatabase = dataContainer.SqlDialogSubject as Database;
                            if (smoDatabase != null)
                            {
                                ((DatabaseInfo)databaseViewInfo.ObjectInfo).Files = GetDatabaseFiles(smoDatabase);
                            }
                        }
                    }

                    // Skip adding logins if running against an Azure SQL DB
                    if (!isAzureDB)
                    {
                        var logins = new List<string>();
                        foreach (Login login in dataContainer.Server.Logins)
                        {
                            logins.Add(login.Name);
                        }
                        // Add <default> to the start of the list in addition to defined logins
                        logins.Insert(0, SR.general_default);

                        databaseViewInfo.LoginNames = new OptionsCollection() { Options = logins.ToArray(), DefaultValueIndex = 0 };
                    }

                    // Restore Database
                    if (!isAzureDB)
                    {
                        RestoreDatabaseTaskDataObject restoreDataObject = new RestoreDatabaseTaskDataObject(dataContainer.Server, requestParams.Database);
                        restoreDataObject.RestoreParams = new RestoreParams();
                        restoreDataObject.RestoreParams.SourceDatabaseName = requestParams.Database;
                        restoreDataObject.RestoreParams.TargetDatabaseName = requestParams.Database;
                        restoreDataObject.RestoreParams.ReadHeaderFromMedia = false;

                        RestoreDatabaseHelper restoreDatabaseService = new RestoreDatabaseHelper();
                        RestorePlanResponse restorePlanResponse = restoreDatabaseService.CreateRestorePlanResponse(restoreDataObject);
                        ((DatabaseInfo)databaseViewInfo.ObjectInfo).restorePlanResponse = restorePlanResponse;
                        RestoreUtil restoreUtil = new RestoreUtil(dataContainer.Server);

                        databaseViewInfo.RestoreDatabaseInfo = new RestoreDatabaseInfo();
                        databaseViewInfo.RestoreDatabaseInfo.TargetDatabaseNames = restoreUtil.GetTargetDbNames().ToArray();
                        databaseViewInfo.RestoreDatabaseInfo.SourceDatabaseNames = restorePlanResponse.DatabaseNamesFromBackupSets;
                        databaseViewInfo.RestoreDatabaseInfo.RecoveryStateOptions = displayRecoveryStateOptions;
                    }

                    var context = new DatabaseViewContext(requestParams);
                    return Task.FromResult(new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context });
                }
            }
        }

        public override Task Save(DatabaseViewContext context, DatabaseInfo obj)
        {
            ConfigureDatabase(
                context.Parameters,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.RunNow);
            return Task.CompletedTask;
        }

        public override Task<string> Script(DatabaseViewContext context, DatabaseInfo obj)
        {
            var script = ConfigureDatabase(
                context.Parameters,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.ScriptToWindow);
            return Task.FromResult(script);
        }

        /// <summary>
        /// Used to detach the specified database from a server.
        /// </summary>
        /// <param name="detachParams">The various parameters needed for the Detach operation</param>
        public string Detach(DetachDatabaseRequestParams detachParams)
        {
            var sqlScript = string.Empty;
            using (var dataContainer = CreateDatabaseDataContainer(detachParams.ConnectionUri, false, detachParams.Database))
            {
                var smoDatabase = dataContainer.SqlDialogSubject as Database;
                if (smoDatabase != null)
                {
                    if (detachParams.GenerateScript)
                    {
                        sqlScript = CreateDetachScript(detachParams, smoDatabase.Name);
                    }
                    else
                    {
                        DatabaseUserAccess originalAccess = smoDatabase.DatabaseOptions.UserAccess;
                        try
                        {
                            // In order to drop all connections to the database, we switch it to single
                            // user access mode so that only our current connection to the database stays open.
                            // Any pending operations are terminated and rolled back.
                            if (detachParams.DropConnections)
                            {
                                smoDatabase.Parent.KillAllProcesses(smoDatabase.Name);
                                smoDatabase.DatabaseOptions.UserAccess = SqlServer.Management.Smo.DatabaseUserAccess.Single;
                                smoDatabase.Alter(TerminationClause.RollbackTransactionsImmediately);
                            }
                            else
                            {
                                // Clear any other connections in the same pool to prevent Detach from hanging
                                // due to the database still being in use
                                var sqlConn = dataContainer.ServerConnection.SqlConnectionObject;
                                SqlConnection.ClearPool(sqlConn);
                            }
                            smoDatabase.Parent.DetachDatabase(smoDatabase.Name, detachParams.UpdateStatistics);
                        }
                        catch (SmoException)
                        {
                            // Revert to database's previous user access level if we changed it as part of dropping connections
                            // before hitting this exception.
                            if (originalAccess != smoDatabase.DatabaseOptions.UserAccess)
                            {
                                smoDatabase.DatabaseOptions.UserAccess = originalAccess;
                                smoDatabase.Alter(TerminationClause.RollbackTransactionsImmediately);
                            }
                            throw;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Could not find database '{detachParams.Database}'.");
                }
            }
            return sqlScript;
        }

        private string CreateDetachScript(DetachDatabaseRequestParams detachParams, string databaseName)
        {
            var escapedName = ToSqlScript.FormatIdentifier(databaseName);
            var builder = new StringBuilder();
            builder.AppendLine("USE [master]");
            builder.AppendLine("GO");
            if (detachParams.DropConnections)
            {
                builder.AppendLine($"ALTER DATABASE {escapedName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                builder.AppendLine("GO");
            }
            builder.Append($"EXEC master.dbo.sp_detach_db @dbname = N'{databaseName}'");
            if (detachParams.UpdateStatistics)
            {
                builder.Append($", @skipchecks = 'false'");
            }
            builder.AppendLine();
            builder.AppendLine("GO");
            return builder.ToString();
        }

        public string Attach(AttachDatabaseRequestParams attachParams)
        {
            var sqlScript = string.Empty;
            ConnectionInfo connectionInfo = this.GetConnectionInfo(attachParams.ConnectionUri);
            using (var dataContainer = CreateDatabaseDataContainer(attachParams.ConnectionUri, true, string.Empty))
            {
                var server = dataContainer.Server!;
                var originalExecuteMode = server.ConnectionContext.SqlExecutionModes;
                if (attachParams.GenerateScript)
                {
                    server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                    server.ConnectionContext.CapturedSql.Clear();
                }
                try
                {
                    foreach (var database in attachParams.Databases)
                    {
                        var fileCollection = new StringCollection();
                        fileCollection.AddRange(database.DatabaseFilePaths);
                        if (database.Owner != SR.general_default)
                        {
                            server.AttachDatabase(database.DatabaseName, fileCollection, database.Owner);
                        }
                        else
                        {
                            server.AttachDatabase(database.DatabaseName, fileCollection);
                        }
                    }
                    if (attachParams.GenerateScript)
                    {
                        var builder = new StringBuilder();
                        var capturedText = server.ConnectionContext.CapturedSql.Text;
                        foreach (var entry in capturedText)
                        {
                            if (entry != null)
                            {
                                builder.AppendLine(entry);
                            }
                        }
                        sqlScript = builder.ToString();
                    }
                }
                finally
                {
                    if (attachParams.GenerateScript)
                    {
                        server.ConnectionContext.SqlExecutionModes = originalExecuteMode;
                    }
                }
            }
            return sqlScript;
        }

        /// <summary>
        /// Used to drop the specified database
        /// </summary>
        /// <param name="dropParams">The various parameters needed for the Drop operation</param>
        public string Drop(DropDatabaseRequestParams dropParams)
        {
            var sqlScript = string.Empty;
            using (var dataContainer = CreateDatabaseDataContainer(dropParams.ConnectionUri, false, dropParams.Database))
            {
                var smoDatabase = dataContainer.SqlDialogSubject as Database;
                if (smoDatabase != null)
                {
                    var originalAccess = smoDatabase.DatabaseOptions.UserAccess;
                    var server = smoDatabase.Parent;
                    var originalExecuteMode = server.ConnectionContext.SqlExecutionModes;

                    if (dropParams.GenerateScript)
                    {
                        server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                        server.ConnectionContext.CapturedSql.Clear();
                    }

                    try
                    {
                        // In order to drop all connections to the database, we switch it to single
                        // user access mode so that only our current connection to the database stays open.
                        // Any pending operations are terminated and rolled back.
                        if (dropParams.DropConnections)
                        {
                            smoDatabase.DatabaseOptions.UserAccess = SqlServer.Management.Smo.DatabaseUserAccess.Single;
                            smoDatabase.Alter(TerminationClause.RollbackTransactionsImmediately);
                        }
                        else
                        {
                            // Clear any other connections in the same pool to prevent Drop from hanging
                            // due to the database still being in use
                            var sqlConn = dataContainer.ServerConnection.SqlConnectionObject;
                            SqlConnection.ClearPool(sqlConn);
                        }
                        smoDatabase.Drop();
                        if (dropParams.DeleteBackupHistory)
                        {
                            server.DeleteBackupHistory(smoDatabase.Name);
                        }
                        if (dropParams.GenerateScript)
                        {
                            var builder = new StringBuilder();
                            foreach (var scriptEntry in server.ConnectionContext.CapturedSql.Text)
                            {
                                if (scriptEntry != null)
                                {
                                    builder.AppendLine(scriptEntry);
                                    builder.AppendLine("GO");
                                }
                            }
                            sqlScript = builder.ToString();
                        }
                    }
                    catch (SmoException)
                    {
                        // Revert to database's previous user access level if we changed it as part of dropping connections
                        // before hitting this exception.
                        if (originalAccess != smoDatabase.DatabaseOptions.UserAccess)
                        {
                            smoDatabase.DatabaseOptions.UserAccess = originalAccess;
                            smoDatabase.Alter(TerminationClause.RollbackTransactionsImmediately);
                        }
                        throw;
                    }
                    finally
                    {
                        if (dropParams.GenerateScript)
                        {
                            server.ConnectionContext.SqlExecutionModes = originalExecuteMode;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Could not find database '{dropParams.Database}'.");
                }
            }
            return sqlScript;
        }

        /// <summary>
        /// Clears all query store data from the database
        /// </summary>
        /// <param name="purgeParams"></param>
        public void PurgeQueryStoreData(PurgeQueryStoreDataRequestParams purgeParams)
        {
            using (var dataContainer = CreateDatabaseDataContainer(purgeParams.ConnectionUri, false, purgeParams.Database))
            {
                var smoDatabase = dataContainer.SqlDialogSubject as Database;
                if (smoDatabase != null)
                {
                    smoDatabase.QueryStoreOptions.PurgeQueryStoreData();
                }
            }
        }

        private CDataContainer CreateDatabaseDataContainer(string connectionUri, bool isNewDatabase, string databaseName)
        {
            ConnectionInfo connectionInfo = this.GetConnectionInfo(connectionUri);
            var originalDatabaseName = connectionInfo.ConnectionDetails.DatabaseName;
            try
            {
                string objectURN;
                if (!isNewDatabase && !string.IsNullOrEmpty(databaseName))
                {
                    connectionInfo.ConnectionDetails.DatabaseName = databaseName;
                    objectURN = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server/Database[@Name='{0}']", Urn.EscapeString(databaseName));
                }
                else
                {
                    objectURN = "Server";
                }
                CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: !isNewDatabase);
                if (dataContainer.Server == null)
                {
                    throw new InvalidOperationException(serverNotExistsError);
                }

                dataContainer.SqlDialogSubject = dataContainer.Server.GetSmoObject(objectURN);
                return dataContainer;
            }
            finally
            {
                connectionInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }
        }

        private string ConfigureDatabase(InitializeViewRequestParams viewParams, DatabaseInfo database, ConfigAction configAction, RunType runType)
        {
            if (database.Name == null)
            {
                throw new ArgumentException("Database name not provided.");
            }

            using (var dataContainer = CreateDatabaseDataContainer(viewParams.ConnectionUri, viewParams.IsNewObject, viewParams.Database))
            {
                if (dataContainer.Server == null)
                {
                    throw new InvalidOperationException(serverNotExistsError);
                }
                using (var taskHelper = new DatabaseTaskHelper(dataContainer))
                {
                    DatabasePrototype prototype = taskHelper.Prototype;
                    prototype.Name = database.Name;

                    // Update database file names now that we have a database name
                    if (viewParams.IsNewObject && !prototype.HideFileSettings)
                    {
                        var sanitizedName = Utility.DatabaseUtils.SanitizeDatabaseFileName(prototype.Name);

                        var dataFile = prototype.Files[0];
                        if (dataFile.DatabaseFileType != FileType.Data)
                        {
                            throw new InvalidOperationException("Database prototype's first file was not a Data file.");
                        }
                        dataFile.Name = sanitizedName;

                        if (prototype.NumberOfLogFiles > 0)
                        {
                            var logFile = prototype.Files[1];
                            if (logFile.DatabaseFileType != FileType.Log)
                            {
                                throw new InvalidOperationException("Database prototype's second file was not a Log file.");
                            }
                            logFile.Name = $"{sanitizedName}_log";
                        }
                    }

                    if (database.Owner != null && database.Owner != SR.general_default)
                    {
                        prototype.Owner = database.Owner;
                    }
                    if (database.CollationName != null)
                    {
                        prototype.Collation = database.CollationName;
                    }
                    if (database.RecoveryModel != null)
                    {
                        prototype.RecoveryModel = recoveryModelEnums[database.RecoveryModel];
                    }
                    if (database.CompatibilityLevel != null)
                    {
                        prototype.DatabaseCompatibilityLevel = compatLevelEnums[database.CompatibilityLevel];
                    }
                    if (prototype is DatabasePrototype80 db80)
                    {
                        if (database.DatabaseReadOnly != null)
                        {
                            db80.IsReadOnly = (bool)database.DatabaseReadOnly;
                        }
                    }

                    if (prototype is DatabasePrototype90 db90)
                    {
                        db90.AutoUpdateStatisticsAsync = database.AutoUpdateStatisticsAsynchronously;
                        db90.PageVerifyDisplay = database.PageVerify;
                    }
                    if (prototype is DatabasePrototype100 db100)
                    {
                        db100.EncryptionEnabled = database.EncryptionEnabled;
                    }
                    if (prototype is DatabasePrototype110 db110)
                    {
                        if (database.TargetRecoveryTimeInSec != null)
                        {
                            db110.TargetRecoveryTime = (int)database.TargetRecoveryTimeInSec;
                        }

                        if (database.ContainmentType != null)
                        {
                            db110.DatabaseContainmentType = containmentTypeEnums[database.ContainmentType];
                        }
                    }
                    if (prototype is DatabasePrototype130 db130)
                    {
                        if (!viewParams.IsNewObject && databaseScopedConfigurationsCollection != null && database.DatabaseScopedConfigurations != null)
                        {
                            foreach (DatabaseScopedConfigurationsInfo dsc in database.DatabaseScopedConfigurations)
                            {
                                foreach (DatabaseScopedConfiguration smoDscCollection in databaseScopedConfigurationsCollection)
                                {
                                    if (smoDscCollection.Name == dsc.Name)
                                    {
                                        smoDscCollection.Value = dsc.ValueForPrimary == CommonConstants.DatabaseScopedConfigurations_Value_Enabled
                                            ? "1" : dsc.ValueForPrimary == CommonConstants.DatabaseScopedConfigurations_Value_Disabled
                                            ? "0" : dsc.ValueForPrimary;

                                        // When sending the DSC seconday value to ADS, we convert the secondaryValue of 'PRIMARY' to match with primaryValue
                                        // We need to set it back to 'PRIMARY' so that SMO would not generate any unnecessary scripts for unchanged properties
                                        if (!(smoDscCollection.ValueForSecondary == CommonConstants.DatabaseScopedConfigurations_Value_Primary &&
                                            dsc.ValueForPrimary.Equals(dsc.ValueForSecondary)))
                                        {
                                            smoDscCollection.ValueForSecondary = dsc.ValueForSecondary == CommonConstants.DatabaseScopedConfigurations_Value_Enabled
                                                        ? "1" : dsc.ValueForSecondary == CommonConstants.DatabaseScopedConfigurations_Value_Disabled
                                                        ? "0" : dsc.ValueForSecondary;
                                        }
                                        break;
                                    }
                                }
                            }
                            db130.DatabaseScopedConfiguration = databaseScopedConfigurationsCollection;
                        }

                        if (!viewParams.IsNewObject && database.QueryStoreOptions != null)
                        {
                            // Sql Server 2019 and higher supports custom query store capture mode
                            captureModeEnums.TryAdd(SR.querystorecapturemode_custom, QueryStoreCaptureMode.Custom);
                            db130.QueryStoreOptions.DesiredState = operationModeEnums[database.QueryStoreOptions.ActualMode];
                            db130.QueryStoreOptions.DataFlushIntervalInSeconds = database.QueryStoreOptions.DataFlushIntervalInMinutes * 60;
                            db130.QueryStoreOptions.StatisticsCollectionIntervalInMinutes = statisticsCollectionIntervalValues[database.QueryStoreOptions.StatisticsCollectionInterval];
                            db130.QueryStoreOptions.MaxPlansPerQuery = database.QueryStoreOptions.MaxPlansPerQuery;
                            db130.QueryStoreOptions.MaxStorageSizeInMB = database.QueryStoreOptions.MaxSizeInMB;
                            db130.QueryStoreOptions.QueryCaptureMode = captureModeEnums[database.QueryStoreOptions.QueryStoreCaptureMode];
                            db130.QueryStoreOptions.SizeBasedCleanupMode = database.QueryStoreOptions.SizeBasedCleanupMode == SR.queryStoreSizeBasedCleanupMode_Off ? QueryStoreSizeBasedCleanupMode.Off : QueryStoreSizeBasedCleanupMode.Auto;
                            db130.QueryStoreOptions.StaleQueryThresholdInDays = database.QueryStoreOptions.StaleQueryThresholdInDays;
                            if (prototype is DatabasePrototype140 db140 && database.QueryStoreOptions.WaitStatisticsCaptureMode != null)
                            {
                                db140.QueryStoreOptions.WaitStatsCaptureMode = database.QueryStoreOptions.WaitStatisticsCaptureMode == CommonConstants.PropertiesDropdown_Value_On ? QueryStoreWaitStatsCaptureMode.On : QueryStoreWaitStatsCaptureMode.Off;
                            }

                            if (prototype is DatabasePrototype150 db150 && database.QueryStoreOptions.QueryStoreCaptureMode == SR.querystorecapturemode_custom
                                && database.QueryStoreOptions.CapturePolicyOptions != null)
                            {
                                db150.QueryStoreOptions.CapturePolicyExecutionCount = database.QueryStoreOptions.CapturePolicyOptions.ExecutionCount;
                                db150.QueryStoreOptions.CapturePolicyStaleThresholdInHrs = queryStoreStaleThresholdValues[database.QueryStoreOptions.CapturePolicyOptions.StaleThreshold];
                                db150.QueryStoreOptions.CapturePolicyTotalCompileCpuTimeInMS = database.QueryStoreOptions.CapturePolicyOptions.TotalCompileCPUTimeInMS;
                                db150.QueryStoreOptions.CapturePolicyTotalExecutionCpuTimeInMS = database.QueryStoreOptions.CapturePolicyOptions.TotalCompileCPUTimeInMS;

                            }
                        }
                    }

                    if (!viewParams.IsNewObject && database.Files != null)
                    {
                        Dictionary<int, DatabaseFilePrototype> fileDict = new Dictionary<int, DatabaseFilePrototype>();
                        foreach (DatabaseFilePrototype file in prototype.Files)
                        {
                            fileDict[file.ID] = file;
                        }
                        foreach (DatabaseFile file in database.Files)
                        {
                            // Add a New file
                            if (file.Id == 0)
                            {
                                DatabaseFilePrototype newFile = new DatabaseFilePrototype(dataContainer, prototype, fileTypesEnums[file.Type]);
                                newFile.Name = file.Name;
                                newFile.InitialSize = (int)file.SizeInMb;
                                newFile.PhysicalName = file.FileNameWithExtension;
                                newFile.DatabaseFileType = fileTypesEnums[file.Type];
                                newFile.Exists = false;
                                newFile.Autogrowth = GetAutogrowth(prototype, file);
                                if (!string.IsNullOrEmpty(file.Path.Trim()))
                                {
                                    newFile.Folder = Utility.DatabaseUtils.ConvertToLocalMachinePath(Path.GetFullPath(file.Path));
                                }

                                // Log file do not support file groups
                                if (fileTypesEnums[file.Type] != FileType.Log)
                                {
                                    FilegroupPrototype fileGroup = new FilegroupPrototype(prototype);
                                    fileGroup.Name = file.FileGroup;
                                    newFile.FileGroup = fileGroup;
                                }

                                // Add newFile to the prototype files
                                prototype.Files.Add(newFile);
                            }
                            // Edit file properties: updating the existed files with modified data
                            else
                            {
                                DatabaseFilePrototype? existingFile;
                                if (fileDict.TryGetValue(file.Id, out existingFile))
                                {
                                    existingFile.Name = file.Name;
                                    existingFile.InitialSize = (int)file.SizeInMb;
                                    existingFile.Autogrowth = GetAutogrowth(prototype, file);
                                }
                                // Once updated, remove it from the dictionary
                                fileDict.Remove(file.Id);
                            }
                        }

                        // Remove the file
                        foreach (KeyValuePair<int, DatabaseFilePrototype> removedfile in fileDict)
                        {
                            removedfile.Value.Removed = true;
                        }
                    }

                    if (!viewParams.IsNewObject && database.Filegroups != null)
                    {
                        Dictionary<string, FilegroupPrototype> groupNameDict = new Dictionary<string, FilegroupPrototype>();
                        foreach (FilegroupPrototype fileGroup in prototype.Filegroups)
                        {
                            groupNameDict[fileGroup.Name] = fileGroup;
                        }
                        // process row data filegroups
                        foreach (FileGroupSummary fg in database.Filegroups)
                        {
                            if (fg.Id < 0)
                            {
                                FilegroupPrototype newfileGroup = new FilegroupPrototype(prototype);
                                newfileGroup.FileGroupType = fg.Type;
                                newfileGroup.Name = fg.Name;
                                newfileGroup.IsReadOnly = fg.IsReadOnly;
                                newfileGroup.IsDefault = fg.IsDefault;
                                newfileGroup.IsAutogrowAllFiles = fg.IsDefault;
                                prototype.Filegroups.Add(newfileGroup);
                            }
                            else
                            {
                                FilegroupPrototype? existingFileGroup;
                                if (groupNameDict.TryGetValue(fg.Name, out existingFileGroup))
                                {
                                    if (fg.Type != FileGroupType.MemoryOptimizedDataFileGroup)
                                    {
                                        existingFileGroup.IsReadOnly = fg.IsReadOnly;
                                        existingFileGroup.IsDefault = fg.IsDefault;
                                        if (fg.Type != FileGroupType.FileStreamDataFileGroup)
                                        {
                                            existingFileGroup.IsAutogrowAllFiles = fg.AutogrowAllFiles;
                                        }
                                    }
                                    // Once updated, remove it from the dictionary
                                    groupNameDict.Remove(fg.Name);
                                }
                            }
                        }

                        // Remove the filegroups
                        foreach (KeyValuePair<string, FilegroupPrototype> removedFilegroups in groupNameDict)
                        {
                            removedFilegroups.Value.Removed = true;
                        }
                    }

                    // AutoCreateStatisticsIncremental can only be set when AutoCreateStatistics is enabled
                    prototype.AutoCreateStatisticsIncremental = database.AutoCreateIncrementalStatistics;
                    prototype.AutoCreateStatistics = database.AutoCreateStatistics;
                    prototype.AutoShrink = database.AutoShrink;
                    prototype.AutoUpdateStatistics = database.AutoUpdateStatistics;
                    if (database.RestrictAccess != null)
                    {
                        prototype.RestrictAccess = database.RestrictAccess;
                    }

                    if (prototype is DatabasePrototypeAzure dbAz)
                    {
                        // Set edition first since the prototype will fill all the Azure fields with default values
                        if (database.AzureEdition != null)
                        {
                            dbAz.AzureEditionDisplay = database.AzureEdition;
                        }
                        if (database.AzureBackupRedundancyLevel != null)
                        {
                            dbAz.BackupStorageRedundancy = database.AzureBackupRedundancyLevel;
                        }
                        if (database.AzureServiceLevelObjective != null)
                        {
                            dbAz.CurrentServiceLevelObjective = database.AzureServiceLevelObjective;
                        }
                        if (database.AzureMaxSize != null)
                        {
                            dbAz.MaxSize = database.AzureMaxSize;
                        }
                    }

                    string sqlScript = string.Empty;
                    using (var actions = new DatabaseActions(dataContainer, configAction, prototype))
                    using (var executionHandler = new ExecutionHandler(actions))
                    {
                        executionHandler.RunNow(runType, this);
                        if (executionHandler.ExecutionResult == ExecutionMode.Failure)
                        {
                            throw executionHandler.ExecutionFailureException;
                        }

                        if (runType == RunType.ScriptToWindow)
                        {
                            sqlScript = executionHandler.ScriptTextFromLastRun;
                        }
                    }

                    return sqlScript;
                }
            }
        }

        private Autogrowth GetAutogrowth(DatabasePrototype prototype, DatabaseFile file)
        {
            Autogrowth fileAutogrowth = new Autogrowth(prototype);
            fileAutogrowth.IsEnabled = file.IsAutoGrowthEnabled;
            bool isGrowthInPercent = file.AutoFileGrowthType == FileGrowthType.Percent;
            fileAutogrowth.IsGrowthInPercent = isGrowthInPercent;
            fileAutogrowth.GrowthInPercent = isGrowthInPercent ? (int)file.AutoFileGrowth : fileAutogrowth.GrowthInPercent;
            fileAutogrowth.GrowthInMegabytes = !isGrowthInPercent ? (int)file.AutoFileGrowth : fileAutogrowth.GrowthInMegabytes;
            fileAutogrowth.MaximumFileSizeInMegabytes = (int)((0.0 <= file.MaxSizeLimitInMb) ? file.MaxSizeLimitInMb : 0.0);
            fileAutogrowth.IsGrowthRestricted = file.MaxSizeLimitInMb > 0.0;

            return fileAutogrowth;
        }

        /// <summary>
        /// Get supported database collations for this server.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the supported collations and the default collation's index.</returns>
        private OptionsCollection GetCollations(Server server, DatabasePrototype prototype, bool isNewObject)
        {
            var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };
            // Writable collations are not supported for Sphinx and earlier
            if (server.VersionMajor < minimumVersionForWritableCollation)
            {
                return options;
            }

            using (DataTable serverCollationsTable = server.EnumCollations())
            {
                if (serverCollationsTable != null)
                {
                    var collationItems = new List<string>();
                    foreach (DataRow serverCollation in serverCollationsTable.Rows)
                    {
                        string collationName = (string)serverCollation["Name"];
                        collationItems.Add(collationName);
                    }

                    // If this database already exists, then use its collation as the default value.
                    // Otherwise use the server's collation as the default value.
                    string firstCollation = prototype.Exists ? prototype.Collation : server.Collation;
                    int defaultIndex = collationItems.FindIndex(collation => collation.Equals(firstCollation, StringComparison.InvariantCultureIgnoreCase));
                    if (defaultIndex > 0)
                    {
                        options.DefaultValueIndex = defaultIndex;
                    }
                    options.Options = collationItems.ToArray();
                }
            }

            return options;
        }

        /// <summary>
        /// Gets the prototype's current collation.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the prototype's collation and the default collation's index.</returns>
        private OptionsCollection GetCollationsWithPrototypeCollation(DatabasePrototype prototype)
        {
            return new OptionsCollection() { Options = new string[] { prototype.Collation }, DefaultValueIndex = 0 };
        }

        /// <summary>
        /// Get supported database containment types for this server.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the supported containment types and the default containment type's index.</returns>
        private OptionsCollection GetContainmentTypes(Server server, DatabasePrototype prototype)
        {
            var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };

            // Containment types are only supported for Denali and later, and only if the server is not a managed instance
            if (!(SqlMgmtUtils.IsSql11OrLater(server.ServerVersion)) || server.IsAnyManagedInstance())
            {
                return options;
            }

            var containmentTypes = new List<string>();
            ContainmentType dbContainmentType = ContainmentType.None;
            DatabasePrototype110? dp110 = prototype as DatabasePrototype110;

            if (dp110 != null)
            {
                dbContainmentType = dp110.DatabaseContainmentType;
            }

            containmentTypes.Add(displayContainmentTypes[ContainmentType.None]);
            containmentTypes.Add(displayContainmentTypes[ContainmentType.Partial]);

            // Use the prototype's current containment type as the default value
            var defaultIndex = 0;
            switch (dbContainmentType)
            {
                case ContainmentType.None:
                    break;
                case ContainmentType.Partial:
                    defaultIndex = 1;
                    break;
                default:
                    break;
            }
            options.DefaultValueIndex = defaultIndex;
            options.Options = containmentTypes.ToArray();
            return options;
        }

        /// <summary>
        /// Get supported database recovery models for this server.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the supported recovery models and the default recovery model's index.</returns>
        private OptionsCollection GetRecoveryModels(Server server, DatabasePrototype prototype)
        {
            var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };

            // Recovery models are only supported if the server is shiloh or later and is not a Managed Instance
            var recoveryModelEnabled = (minimumVersionForRecoveryModel <= server.VersionMajor) && !server.IsAnyManagedInstance();
            if (server.GetDisabledProperties().Contains("RecoveryModel") || !recoveryModelEnabled)
            {
                return options;
            }

            var recoveryModels = new List<string>();
            if (!server.IsAnyManagedInstance())
            {

                recoveryModels.Add(displayRecoveryModels[RecoveryModel.Full]);
                recoveryModels.Add(displayRecoveryModels[RecoveryModel.BulkLogged]);
                recoveryModels.Add(displayRecoveryModels[RecoveryModel.Simple]);
            }
            else
            {
                if (prototype.OriginalName.Equals("tempdb", StringComparison.CurrentCultureIgnoreCase) && prototype.IsSystemDB)
                {
                    // tempdb supports 'simple recovery' only
                    recoveryModels.Add(displayRecoveryModels[RecoveryModel.Simple]);
                }
                else
                {
                    // non-tempdb supports only 'full recovery' model
                    recoveryModels.Add(displayRecoveryModels[RecoveryModel.Full]);
                }
            }

            // Use the prototype's current recovery model as the default value
            if (recoveryModels.Count > 1)
            {
                var defaultIndex = 0;
                switch (prototype.RecoveryModel)
                {
                    case RecoveryModel.BulkLogged:
                        defaultIndex = 1;
                        break;

                    case RecoveryModel.Simple:
                        defaultIndex = 2;
                        break;

                    default:
                        break;
                }
                options.DefaultValueIndex = defaultIndex;
            }
            options.Options = recoveryModels.ToArray();
            return options;
        }

        private DatabaseFile[] GetDatabaseFiles(Database database)
        {
            var filesList = new List<DatabaseFile>();
            foreach (FileGroup fileGroup in database.FileGroups)
            {
                foreach (DataFile file in fileGroup.Files)
                {
                    filesList.Add(new DatabaseFile()
                    {
                        Id = file.ID,
                        Name = file.Name,
                        Type = file.Parent.FileGroupType == FileGroupType.RowsFileGroup ? displayFileTypes[FileType.Data] : displayFileTypes[FileType.FileStream],
                        Path = Utility.DatabaseUtils.ConvertToLocalMachinePath(Path.GetDirectoryName(file.FileName)),
                        FileGroup = fileGroup.Name,
                        FileNameWithExtension = Path.GetFileName(file.FileName),
                        SizeInMb = ByteConverter.ConvertKbtoMb(file.Size),
                        AutoFileGrowth = file.GrowthType == FileGrowthType.Percent ? file.Growth : ByteConverter.ConvertKbtoMb(file.Growth),
                        AutoFileGrowthType = file.GrowthType,
                        MaxSizeLimitInMb = file.MaxSize == -1 ? file.MaxSize : ByteConverter.ConvertKbtoMb(file.MaxSize),
                        IsAutoGrowthEnabled = file.GrowthType != FileGrowthType.None,
                    });
                }
            }
            foreach (LogFile file in database.LogFiles)
            {
                filesList.Add(new DatabaseFile()
                {
                    Id = file.ID,
                    Name = file.Name,
                    Type = displayFileTypes[FileType.Log],
                    Path = Utility.DatabaseUtils.ConvertToLocalMachinePath(Path.GetDirectoryName(file.FileName)),
                    FileGroup = SR.prototype_file_noFileGroup,
                    FileNameWithExtension = Path.GetFileName(file.FileName),
                    SizeInMb = ByteConverter.ConvertKbtoMb(file.Size),
                    AutoFileGrowth = file.GrowthType == FileGrowthType.Percent ? file.Growth : ByteConverter.ConvertKbtoMb(file.Growth),
                    AutoFileGrowthType = file.GrowthType,
                    MaxSizeLimitInMb = file.MaxSize == -1 ? file.MaxSize : ByteConverter.ConvertKbtoMb(file.MaxSize),
                    IsAutoGrowthEnabled = file.GrowthType != FileGrowthType.None
                });
            }
            return filesList.ToArray();
        }

        /// <summary>
        /// Preparing the filegroups of various FileGroupTypes
        /// </summary>
        /// <param name="database"></param>
        /// <param name="databaseViewInfo"></param>
        private FileGroupSummary[] GetFileGroups(Database database, DatabaseViewInfo databaseViewInfo)
        {
            var filegroups = new List<FileGroupSummary>();
            foreach (FileGroup filegroup in database.FileGroups)
            {
                filegroups.Add(new FileGroupSummary()
                {
                    Id = filegroup.ID,
                    Name = filegroup.Name,
                    Type = filegroup.FileGroupType,
                    IsReadOnly = filegroup.ReadOnly,
                    IsDefault = filegroup.IsDefault
                });
            }
            return filegroups.ToArray();
        }

        /// <summary>
        /// Get supported database compatibility levels for this Azure server.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the supported compatibility levels and the default compatibility level's index.</returns>
        private OptionsCollection GetCompatibilityLevelsAzure(DatabasePrototype prototype)
        {
            var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };
            // For Azure we loop through all of the possible compatibility levels. We do this because there's only one compat level active on a
            // version at a time, but that can change at any point so in order to reduce maintenance required when that happens we'll just find
            // the one that matches the current set level and display that
            foreach (var level in displayCompatLevels.Keys)
            {
                if (level == prototype.DatabaseCompatibilityLevel)
                {
                    // Azure can't change the compat level so we only include the current version
                    options.Options = new string[] { displayCompatLevels[level] };
                    return options;
                }
            }

            // If we couldn't find the prototype's current compatibility level, then treat compatibillity levels as unsupported for this server
            return options;
        }

        /// <summary>
        /// Get supported database compatibility levels for this server.
        /// </summary>
        /// <returns>An <see cref="OptionsCollection"/> of the supported compatibility levels and the default compatibility level's index.</returns>
        private OptionsCollection GetCompatibilityLevels(int sqlServerVersion, DatabasePrototype prototype)
        {
            var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };

            // Unlikely that we are hitting such an old SQL Server, but leaving to preserve
            // the original semantic of this method.
            if (sqlServerVersion < 8)
            {
                // we do not know this version number, we do not know the possible compatibility levels for the server
                return options;
            }

            var compatibilityLevels = new List<string>();
            switch (sqlServerVersion)
            {
                case 8:     // Shiloh
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version70]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version80]);
                    break;
                case 9:     // Yukon
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version70]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version80]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version90]);
                    break;
                case 10:    // Katmai
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version80]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version90]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    break;
                case 11:    // Denali
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version90]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    break;
                case 12:    // SQL2014
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version120]);
                    break;
                case 13:    // SQL2016
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version130]);
                    break;
                case 14:    // SQL2017
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version140]);
                    break;
                case 15:    // SQL2019
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version140]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version150]);
                    break;
                /* SQL_VBUMP_REVIEW */
                default:
                    // It is either the latest SQL we know about, or some future version of SQL we
                    // do not know about. We play conservative and only add the compat level we know
                    // about so far.
                    // At vBump, add a new case and move the 'default' label there.
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version140]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version150]);
                    compatibilityLevels.Add(displayCompatLevels[CompatibilityLevel.Version160]);
                    break;
            }

            // set the default compatability level for this list based on the prototype
            for (var i = 0; i < compatibilityLevels.Count; i++)
            {
                var level = compatibilityLevels[i];
                var prototypeLevel = displayCompatLevels[prototype.DatabaseCompatibilityLevel];
                if (level == prototypeLevel)
                {
                    options.DefaultValueIndex = i;
                    options.Options = compatibilityLevels.ToArray();
                    return options;
                }
            }

            // previous loop did not find the prototype compatibility level in this server's compatability options, so treat compatibility levels as unsupported for this server
            return options;
        }

        /// <summary>
        /// Get supported service level objectives for this Azure server.
        /// </summary>
        private static AzureEditionDetails[] GetAzureServiceLevels(IEnumerable<AzureEdition> editions)
        {
            var levels = new List<AzureEditionDetails>();
            foreach (AzureEdition edition in editions)
            {
                if (AzureSqlDbHelper.TryGetServiceObjectiveInfo(edition, out var serviceInfoPair))
                {
                    var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };
                    var serviceLevelsList = new List<string>(serviceInfoPair.Value);
                    var defaultIndex = serviceInfoPair.Key;
                    if (defaultIndex >= 0 && defaultIndex < serviceLevelsList.Count)
                    {
                        options.DefaultValueIndex = defaultIndex;
                    }
                    options.Options = serviceLevelsList.ToArray();
                    var details = new AzureEditionDetails() { EditionDisplayName = edition.DisplayName, EditionOptions = options };
                    levels.Add(details);
                }
                else
                {
                    Logger.Error($"Failed to get service level objective info for edition '{edition.Name}'");
                }
            }
            return levels.ToArray();
        }

        /// <summary>
        /// Get supported maximum sizes for this Azure server.
        /// </summary>
        private static AzureEditionDetails[] GetAzureMaxSizes(IEnumerable<AzureEdition> editions)
        {
            var sizes = new List<AzureEditionDetails>();
            foreach (AzureEdition edition in editions)
            {
                if (AzureSqlDbHelper.TryGetDatabaseSizeInfo(edition, out var sizeInfoPair))
                {
                    var options = new OptionsCollection() { Options = Array.Empty<string>(), DefaultValueIndex = 0 };
                    var sizeInfoList = new List<DbSize>(sizeInfoPair.Value);
                    var defaultIndex = sizeInfoPair.Key;
                    if (defaultIndex >= 0 && defaultIndex < sizeInfoList.Count)
                    {
                        options.DefaultValueIndex = defaultIndex;
                    }
                    options.Options = sizeInfoList.Select(info => info.ToString()).ToArray();
                    var details = new AzureEditionDetails() { EditionDisplayName = edition.DisplayName, EditionOptions = options };
                    sizes.Add(details);
                }
                else
                {
                    Logger.Error($"Failed to get database size info for edition '{edition.Name}'");
                }
            }
            return sizes.ToArray();
        }

        /// <summary>
        /// Prepares database scoped configurations list
        /// </summary>
        /// <param name="smoDSCMetaData"></param>
        /// <returns>database scoped configurations metadata array</returns>
        private static DatabaseScopedConfigurationsInfo[] GetDSCMetaData(DatabaseScopedConfigurationCollection smoDSCMetaData)
        {
            var dscMetaData = new List<DatabaseScopedConfigurationsInfo>();
            foreach (DatabaseScopedConfiguration dsc in smoDSCMetaData)
            {
                string primaryValue = GetDscValue(dsc.Id, dsc.Value);
                dscMetaData.Add(new DatabaseScopedConfigurationsInfo()
                {
                    Id = dsc.Id,
                    Name = dsc.Name,
                    ValueForPrimary = primaryValue,
                    ValueForSecondary = dsc.ValueForSecondary == CommonConstants.DatabaseScopedConfigurations_Value_Primary ? primaryValue : GetDscValue(dsc.Id, dsc.ValueForSecondary)
                });
            }
            return dscMetaData.ToArray();
        }

        /// <summary>
        /// Gets primary and secondary value of the database scoped configuration property
        /// </summary>
        /// <param name="dsc"></param>
        /// <returns>Value of the primary/secondary</returns>
        private static string GetDscValue(int id, string value)
        {
            // MAXDOP(Id = 1) and PAUSED_RESUMABLE_INDEX_ABORT_DURATION_MINUTES(Id = 25) are integer numbers but coming as string value type and they need to send as is.
            if (id == 1 || id == 25)
            {
                return value;
            }

            switch (value)
            {
                case "1":
                    return CommonConstants.DatabaseScopedConfigurations_Value_Enabled;
                case "0":
                    return CommonConstants.DatabaseScopedConfigurations_Value_Disabled;
                default:
                    return value;
            }
        }
    }
}