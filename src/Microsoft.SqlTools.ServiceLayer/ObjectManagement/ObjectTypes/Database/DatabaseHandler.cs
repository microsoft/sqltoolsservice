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
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Text;
using System.IO;
using Microsoft.SqlTools.ServiceLayer.Utility.SqlScriptFormatters;

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

        private static readonly Dictionary<string, CompatibilityLevel> compatLevelEnums = new Dictionary<string, CompatibilityLevel>();
        private static readonly Dictionary<string, ContainmentType> containmentTypeEnums = new Dictionary<string, ContainmentType>();
        private static readonly Dictionary<string, RecoveryModel> recoveryModelEnums = new Dictionary<string, RecoveryModel>();

        internal static readonly string[] AzureEditionNames;
        internal static readonly string[] AzureBackupLevels;
        internal static readonly AzureEditionDetails[] AzureMaxSizes;
        internal static readonly AzureEditionDetails[] AzureServiceLevels;

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
            using (var dataContainer = CreateDatabaseDataContainer(requestParams.ConnectionUri, requestParams.ObjectUrn, requestParams.IsNewObject, requestParams.Database))
            {
                if (dataContainer.Server == null)
                {
                    throw new InvalidOperationException(serverNotExistsError);
                }
                try
                {
                    using (var taskHelper = new DatabaseTaskHelper(dataContainer))
                    using (var context = new DatabaseViewContext(requestParams))
                    {
                        var prototype = taskHelper.Prototype;
                        var azurePrototype = prototype as DatabasePrototypeAzure;
                        bool isDw = azurePrototype != null && azurePrototype.AzureEdition == AzureEdition.DataWarehouse;
                        bool isAzureDB = dataContainer.Server.ServerType == DatabaseEngineType.SqlAzureDatabase;
                        bool isManagedInstance = dataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;

                        var databaseViewInfo = new DatabaseViewInfo()
                        {
                            ObjectInfo = new DatabaseInfo(),
                            IsAzureDB = isAzureDB
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
                                    ContainmentType = displayContainmentTypes[smoDatabase.ContainmentType],
                                    RecoveryModel = displayRecoveryModels[smoDatabase.RecoveryModel],
                                    DateCreated = smoDatabase.CreateDate.ToString(),
                                    LastDatabaseBackup = smoDatabase.LastBackupDate == DateTime.MinValue ? SR.databaseBackupDate_None : smoDatabase.LastBackupDate.ToString(),
                                    LastDatabaseLogBackup = smoDatabase.LastLogBackupDate == DateTime.MinValue ? SR.databaseBackupDate_None : smoDatabase.LastLogBackupDate.ToString(),
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
                                    EncryptionEnabled = smoDatabase.EncryptionEnabled
                                };

                                if (!isManagedInstance)
                                {
                                    databaseViewInfo.PageVerifyOptions = displayPageVerifyOptions.Values.ToArray();
                                    databaseViewInfo.RestrictAccessOptions = displayRestrictAccessOptions.Values.ToArray();
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).DatabaseReadOnly = smoDatabase.ReadOnly;
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).RestrictAccess = displayRestrictAccessOptions[smoDatabase.UserAccess];
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).PageVerify = displayPageVerifyOptions[smoDatabase.PageVerify];
                                    ((DatabaseInfo)databaseViewInfo.ObjectInfo).TargetRecoveryTimeInSec = smoDatabase.TargetRecoveryTime;

                                    if (prototype is DatabasePrototype160)
                                    {
                                        ((DatabaseInfo)databaseViewInfo.ObjectInfo).IsLedgerDatabase = smoDatabase.IsLedger;
                                    }
                                }
                            }
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
                                    databaseViewInfo.Files = GetDatabaseFiles(smoDatabase);
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

                        return Task.FromResult(new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context });
                    }
                }
                finally
                {
                    ServerConnection serverConnection = dataContainer.Server.ConnectionContext;
                    if (serverConnection.IsOpen)
                    {
                        serverConnection.Disconnect();
                    }
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
            ConnectionInfo connectionInfo = this.GetConnectionInfo(detachParams.ConnectionUri);
            using (var dataContainer = CreateDatabaseDataContainer(detachParams.ConnectionUri, detachParams.ObjectUrn, false, null))
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
                    throw new InvalidOperationException($"Provided URN '{detachParams.ObjectUrn}' did not correspond to an existing database.");
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

        private CDataContainer CreateDatabaseDataContainer(string connectionUri, string? objectURN, bool isNewDatabase, string? databaseName)
        {
            ConnectionInfo connectionInfo = this.GetConnectionInfo(connectionUri);
            if (!isNewDatabase && !string.IsNullOrEmpty(databaseName))
            {
                connectionInfo.ConnectionDetails.DatabaseName = databaseName;
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: !isNewDatabase);
            if (dataContainer.Server == null)
            {
                throw new InvalidOperationException(serverNotExistsError);
            }
            if (string.IsNullOrEmpty(objectURN))
            {
                objectURN = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server");
            }
            dataContainer.SqlDialogSubject = dataContainer.Server.GetSmoObject(objectURN);
            return dataContainer;
        }

        private string ConfigureDatabase(InitializeViewRequestParams viewParams, DatabaseInfo database, ConfigAction configAction, RunType runType)
        {
            if (database.Name == null)
            {
                throw new ArgumentException("Database name not provided.");
            }

            using (var dataContainer = CreateDatabaseDataContainer(viewParams.ConnectionUri, viewParams.ObjectUrn, viewParams.IsNewObject, viewParams.Database))
            {
                if (dataContainer.Server == null)
                {
                    throw new InvalidOperationException(serverNotExistsError);
                }
                try
                {
                    using (var taskHelper = new DatabaseTaskHelper(dataContainer))
                    {
                        DatabasePrototype prototype = taskHelper.Prototype;
                        prototype.Name = database.Name;

                        // Update database file names now that we have a database name
                        if (viewParams.IsNewObject && !prototype.HideFileSettings)
                        {
                            var sanitizedName = DatabaseUtils.SanitizeDatabaseFileName(prototype.Name);

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

                        if (database.Owner != null && database.Owner != SR.general_default && viewParams.IsNewObject)
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
                        using (var executionHandler = new ExecutonHandler(actions))
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
                finally
                {
                    ServerConnection serverConnection = dataContainer.Server.ConnectionContext;
                    if (serverConnection.IsOpen)
                    {
                        serverConnection.Disconnect();
                    }
                }
            }
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
                        Name = file.Name,
                        Type = FileType.Data.ToString(),
                        Path = Path.GetDirectoryName(file.FileName),
                        FileGroup = fileGroup.Name
                    });
                }
            }
            foreach (LogFile file in database.LogFiles)
            {
                filesList.Add(new DatabaseFile()
                {
                    Name = file.Name,
                    Type = FileType.Log.ToString(),
                    Path = Path.GetDirectoryName(file.FileName),
                    FileGroup = string.Empty
                });
            }
            return filesList.ToArray();
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
    }
}