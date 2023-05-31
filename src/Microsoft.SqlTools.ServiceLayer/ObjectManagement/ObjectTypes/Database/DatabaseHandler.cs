//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using static Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

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

        private static readonly Dictionary<string, CompatibilityLevel> compatLevelEnums = new Dictionary<string, CompatibilityLevel>();
        private static readonly Dictionary<string, ContainmentType> containmentTypeEnums = new Dictionary<string, ContainmentType>();
        private static readonly Dictionary<string, RecoveryModel> recoveryModelEnums = new Dictionary<string, RecoveryModel>();

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
            using (var dataContainer = CreateDatabaseDataContainer(requestParams.ConnectionUri, ConfigAction.Create))
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

                        var databaseViewInfo = new DatabaseViewInfo()
                        {
                            ObjectInfo = new DatabaseInfo()
                        };

                        // azure sql db doesn't have a sysadmin fixed role
                        var compatibilityLevelEnabled = !isDw &&
                                                        (dataContainer.LoggedInUserIsSysadmin ||
                                                        dataContainer.Server.ServerType ==
                                                        DatabaseEngineType.SqlAzureDatabase);
                        if (dataContainer.Server.ServerType == DatabaseEngineType.SqlAzureDatabase)
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
                        }

                        // Skip adding logins if running against an Azure SQL DB
                        if (dataContainer.Server.ServerType != DatabaseEngineType.SqlAzureDatabase)
                        {
                            var logins = new List<string>();
                            logins.Add(SR.general_default);
                            foreach (Login login in dataContainer.Server.Logins)
                            {
                                logins.Add(login.Name);
                            }
                            databaseViewInfo.LoginNames = logins.ToArray();
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
                context.Parameters.ConnectionUri,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.RunNow);
            return Task.CompletedTask;
        }

        public override Task<string> Script(DatabaseViewContext context, DatabaseInfo obj)
        {
            var script = ConfigureDatabase(
                context.Parameters.ConnectionUri,
                obj,
                context.Parameters.IsNewObject ? ConfigAction.Create : ConfigAction.Update,
                RunType.ScriptToWindow);
            return Task.FromResult(script);
        }

        private CDataContainer CreateDatabaseDataContainer(string connectionUri, ConfigAction configAction, DatabaseInfo? database = null)
        {
            ConnectionInfo connectionInfo = this.GetConnectionInfo(connectionUri);
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: configAction != ConfigAction.Create);
            if (dataContainer.Server == null)
            {
                throw new InvalidOperationException(serverNotExistsError);
            }
            string objectUrn = (configAction != ConfigAction.Create && database != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(database.Name))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server");
            dataContainer.SqlDialogSubject = dataContainer.Server.GetSmoObject(objectUrn);
            return dataContainer;
        }

        private string ConfigureDatabase(string connectionUri, DatabaseInfo database, ConfigAction configAction, RunType runType)
        {
            if (database.Name == null)
            {
                throw new ArgumentException("Database name not provided.");
            }

            using (var dataContainer = CreateDatabaseDataContainer(connectionUri, configAction, database))
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
                        if (!prototype.HideFileSettings)
                        {
                            var sanitizedName = DatabaseUtils.SanitizeDatabaseFileName(prototype.Name);

                            var dataFile = prototype.Files[0];
                            Debug.Assert(dataFile.DatabaseFileType == FileType.Data, "Expected first database file to be a data file for new database prototype.");
                            dataFile.Name = sanitizedName;

                            if (prototype.NumberOfLogFiles > 0)
                            {
                                var logFile = prototype.Files[1];
                                Debug.Assert(dataFile.DatabaseFileType == FileType.Log, "Expected first database file to be a log file for new database prototype");
                                logFile.Name = $"{sanitizedName}_log";
                            }
                        }

                        if (database.Owner != null && database.Owner != SR.general_default)
                        {
                            prototype.Owner = database.Owner;
                        }
                        if (database.CollationName != null && database.CollationName != SR.general_default)
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
                        if (prototype is DatabasePrototype110 db110 && database.ContainmentType != null)
                        {
                            db110.DatabaseContainmentType = containmentTypeEnums[database.ContainmentType];
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

        private bool IsManagedInstance(Server server)
        {
            return server?.Information?.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;
        }

        private bool IsArcEnabledManagedInstance(Server server)
        {
            return server?.Information?.DatabaseEngineEdition == DatabaseEngineEdition.SqlAzureArcManagedInstance;
        }

        private bool IsAnyManagedInstance(Server server)
        {
            return (IsManagedInstance(server) || IsArcEnabledManagedInstance(server));
        }

        private string[] GetCollations(Server server, DatabasePrototype prototype, bool isNewObject)
        {
            var collationItems = new List<string>();
            bool isSphinxServer = (server.VersionMajor < minimumVersionForWritableCollation);

            // if we're creating a new database or this is a Sphinx Server, add "<default>" to the list
            if (isNewObject || isSphinxServer)
            {
                collationItems.Add(SR.general_default);
            }

            // if the server is shiloh or later, add specific collations to the list
            if (!isSphinxServer)
            {
                DataTable serverCollationsTable = server.EnumCollations();
                if (serverCollationsTable != null)
                {
                    foreach (DataRow serverCollation in serverCollationsTable.Rows)
                    {
                        string collationName = (string)serverCollation["Name"];
                        collationItems.Add(collationName);
                    }
                }
            }

            if (prototype.Exists)
            {
                System.Diagnostics.Debug.Assert(((prototype.Collation != null) && (prototype.Collation.Length != 0)),
                    "prototype.Collation is null");
                System.Diagnostics.Debug.Assert(collationItems.Contains(prototype.Collation),
                    "prototype.Collation is not in the collation list");

                int index = collationItems.FindIndex(collation => collation.Equals(prototype.Collation, StringComparison.InvariantCultureIgnoreCase));
                if (index > 0)
                {
                    collationItems.RemoveAt(index);
                    collationItems.Insert(0, prototype.Collation);
                }
            }
            return collationItems.ToArray();
        }

        private string[] GetCollationsWithPrototypeCollation(DatabasePrototype prototype)
        {
            return new string[] { prototype.Collation };
        }

        private string[] GetContainmentTypes(Server server, DatabasePrototype prototype)
        {
            if (!(SqlMgmtUtils.IsSql11OrLater(server.ServerVersion)) || IsAnyManagedInstance(server))
            {
                return Array.Empty<string>();
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

            var swapIndex = 0;
            switch (dbContainmentType)
            {
                case ContainmentType.None:
                    break;
                case ContainmentType.Partial:
                    swapIndex = 1;
                    break;
                default:
                    Debug.Fail(string.Format(CultureInfo.InvariantCulture, "Unexpected containment type '{0}'", dbContainmentType));
                    break;
            }
            if (swapIndex > 0)
            {
                var value = containmentTypes[swapIndex];
                containmentTypes.RemoveAt(swapIndex);
                containmentTypes.Insert(0, value);
            }

            return containmentTypes.ToArray();
        }

        private string[] GetRecoveryModels(Server server, DatabasePrototype prototype)
        {
            // Recovery models are only supported if the server is shiloh or later and is not a Managed Instance
            var recoveryModelEnabled = (minimumVersionForRecoveryModel <= server.VersionMajor) && !IsAnyManagedInstance(server);
            if (server.GetDisabledProperties().Contains("RecoveryModel") || !recoveryModelEnabled)
            {
                return Array.Empty<string>();
            }

            var recoveryModels = new List<string>();
            if (!IsAnyManagedInstance(server))
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

            if (recoveryModelEnabled)
            {
                var swapIndex = 0;
                switch (prototype.RecoveryModel)
                {
                    case RecoveryModel.BulkLogged:
                        swapIndex = 1;
                        break;

                    case RecoveryModel.Simple:
                        swapIndex = 2;
                        break;

                    default:
                        Debug.Assert(RecoveryModel.Full == prototype.RecoveryModel, string.Format(CultureInfo.InvariantCulture, "Unknown recovery model '{0}'", prototype.RecoveryModel));
                        break;
                }
                if (swapIndex > 0)
                {
                    var value = recoveryModels[swapIndex];
                    recoveryModels.RemoveAt(swapIndex);
                    recoveryModels.Insert(0, value);
                }
            }
            return recoveryModels.ToArray();
        }

        private string[] GetCompatibilityLevelsAzure(DatabasePrototype prototype)
        {
            // For Azure we loop through all of the possible compatibility levels. We do this because there's only one compat level active on a
            // version at a time, but that can change at any point so in order to reduce maintenance required when that happens we'll just find
            // the one that matches the current set level and display that
            foreach (var level in displayCompatLevels.Keys)
            {
                if (level == prototype.DatabaseCompatibilityLevel)
                {
                    // Azure can't change the compat level so we only include the current version
                    return new string[] { displayCompatLevels[level] };
                }
            }

            Debug.Fail(string.Format(CultureInfo.InvariantCulture, "Unknown compatibility level '{0}'", prototype.DatabaseCompatibilityLevel));
            return Array.Empty<string>();
        }

        private string[] GetCompatibilityLevels(int sqlServerVersion, DatabasePrototype prototype)
        {
            // Unlikely that we are hitting such an old SQL Server, but leaving to preserve
            // the original semantic of this method.
            if (sqlServerVersion < 8)
            {
                // we do not know this version number, we do not know the possible compatibility levels for the server
                return Array.Empty<string>();
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

            // set the first compatability level for this list based on the prototype
            for (var i = 0; i < compatibilityLevels.Count; i++)
            {
                var level = compatibilityLevels[i];
                var prototypeLevel = displayCompatLevels[prototype.DatabaseCompatibilityLevel];
                if (level == prototypeLevel)
                {
                    if (i > 0)
                    {
                        compatibilityLevels.RemoveAt(i);
                        compatibilityLevels.Insert(0, level);
                    }
                    return compatibilityLevels.ToArray();
                }
            }

            // previous loop did not find the prototype compatibility level in this server's compatability options, so treat compatibility levels as unsupported for this server
            return Array.Empty<string>();
        }
    }
}