//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Admin;
using static Microsoft.SqlTools.ServiceLayer.Admin.AzureSqlDbHelper;
using System.Resources;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Login object type handler
    /// </summary>
    public class DatabaseHandler : ObjectTypeHandler<DatabaseInfo, DatabaseViewContext>
    {
        private const int minimumVersionForWritableCollation = 8;
        private const int minimumVersionForRecoveryModel = 8;
        private readonly ResourceManager resourceManager;

        /// <summary>
        /// Set of valid compatibility levels and their display strings
        /// </summary>
        private Dictionary<CompatibilityLevel, string> compatLevels = new Dictionary<CompatibilityLevel, string>()
        {
            {CompatibilityLevel.Version70, "SQL Server 7.0 (70)"},
            {CompatibilityLevel.Version80, "SQL Server 2000 (80)"},
            {CompatibilityLevel.Version90, "SQL Server 2005 (90)"},
            {CompatibilityLevel.Version100, "SQL Server 2008 (100)"},
            {CompatibilityLevel.Version110, "SQL Server 2012 (110)"},
            {CompatibilityLevel.Version120, "SQL Server 2014 (120)"},
            {CompatibilityLevel.Version130, "SQL Server 2016 (130)"},
            {CompatibilityLevel.Version140, "SQL Server 2017 (140)"},
            {CompatibilityLevel.Version150, "SQL Server 2019 (150)"},
            {CompatibilityLevel.Version160, "SQL Server 2022 (160)"},
        };

        private Dictionary<string, CompatibilityLevel> displayCompatLevels = new Dictionary<string, CompatibilityLevel>()
        {
            {"SQL Server 7.0 (70)", CompatibilityLevel.Version70},
            {"SQL Server 2000 (80)", CompatibilityLevel.Version80},
            {"SQL Server 2005 (90)", CompatibilityLevel.Version90},
            {"SQL Server 2008 (100)", CompatibilityLevel.Version100},
            {"SQL Server 2012 (110)", CompatibilityLevel.Version110},
            {"SQL Server 2014 (120)", CompatibilityLevel.Version120},
            {"SQL Server 2016 (130)", CompatibilityLevel.Version130},
            {"SQL Server 2017 (140)", CompatibilityLevel.Version140},
            {"SQL Server 2019 (150)", CompatibilityLevel.Version150},
            {"SQL Server 2022 (160)", CompatibilityLevel.Version160},
        };

        public DatabaseHandler(ConnectionService connectionService) : base(connectionService)
        {
            this.resourceManager = new ResourceManager("Microsoft.SqlTools.ServiceLayer.Localization.SR", typeof(DatabasePrototype).GetAssembly());
        }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.Database;
        }

        public async override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams requestParams)
        {
            // open a connection for running the user dialog and associated task
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(requestParams.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", requestParams.ConnectionUri);
            }
            string originalDatabaseName = originalConnInfo.ConnectionDetails.DatabaseName;
            try
            {
                originalConnInfo.ConnectionDetails.DatabaseName = requestParams.Database;
                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = requestParams.ContextId,
                    Connection = originalConnInfo.ConnectionDetails,
                    Type = Connection.ConnectionType.Default
                };
                await this.ConnectionService.Connect(connectParams);
            }
            finally
            {
                originalConnInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(requestParams.ContextId, out connInfo);

            // create a default user data context and database object
            CDataContainer dataContainer = CreateDatabaseDataContainer(connInfo, null, ConfigAction.Create, requestParams.Database);
            var parentServer = dataContainer.Server;
            var prototype = CreateDatabasePrototype(dataContainer);
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
                        databaseViewInfo.CollationNames = PopulateCollationDropdownWithPrototypeCollation(prototype);
                    }
                    databaseViewInfo.CompatibilityLevels = PopulateCompatibilityLevelDropdownAzure(dataContainer, prototype);
                }
                else
                {
                    if (collationEnabled)
                    {
                        databaseViewInfo.CollationNames = PopulateCollationDropdown(dataContainer, prototype);
                    }
                    if (compatibilityLevelEnabled)
                    {
                        databaseViewInfo.CompatibilityLevels = PopulateCompatibilityLevelDropdown(dataContainer, prototype);
                    }
                }
            }
            else
            {
                databaseViewInfo.CollationNames = PopulateCollationDropdown(dataContainer, prototype);
                if (compatibilityLevelEnabled)
                {
                    databaseViewInfo.CompatibilityLevels = PopulateCompatibilityLevelDropdown(dataContainer, prototype);
                }

                // These aren't visible when the target DB is on Azure so only populate if it's not an Azure DB
                databaseViewInfo.RecoveryModels = PopulateRecoveryModelDropdown(dataContainer, prototype);
                databaseViewInfo.ContainmentTypes = PopulateContainmentTypeDropdown(dataContainer, prototype);
            }

            var databases = new List<string>();
            foreach (Database database in parentServer.Databases)
            {
                databases.Add(database.Name);
            }
            databaseViewInfo.DatabaseNames = databases.ToArray();

            var logins = new List<string>();
            foreach (Login login in parentServer.Logins)
            {
                logins.Add(login.Name);
            }
            databaseViewInfo.LoginNames = logins.ToArray();

            var context = new DatabaseViewContext(requestParams);
            return new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context };
        }

        private CDataContainer CreateDatabaseDataContainer(ConnectionInfo connInfo, DatabaseInfo database, ConfigAction configAction, string databaseName)
        {
            var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");
            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;

            string urn = (configAction == ConfigAction.Update && database != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "Server");

            ActionContext context = new ActionContext(serverConnection, "Database", urn);
            DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);

            if (configAction == ConfigAction.Create)
            {
                containerXml.AddProperty("itemtype", "Database");
            }

            XmlDocument xmlDoc = containerXml.GenerateXmlDocument();
            return CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);
        }

        public override Task Save(DatabaseViewContext context, DatabaseInfo obj)
        {
            var updateExistingDB = !context.Parameters.IsNewObject;
            var script = this.HandleDatabaseRequest(context, obj, RunType.RunNow, updateExistingDB);
            return Task.CompletedTask;
        }

        public override Task<string> Script(DatabaseViewContext context, DatabaseInfo obj)
        {
            var updateExistingDB = !context.Parameters.IsNewObject;
            var script = this.HandleDatabaseRequest(context, obj, RunType.ScriptToWindow, updateExistingDB);
            return Task.FromResult(script);
        }

        private string HandleDatabaseRequest(DatabaseViewContext context, DatabaseInfo database, RunType runType, bool updateExistingDB)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: updateExistingDB);
            DatabasePrototype prototype = CreateDatabasePrototype(dataContainer);

            prototype.Name = database.Name;
            prototype.Owner = database.Owner;
            prototype.Collation = database.CollationName;
            prototype.RecoveryModel = Enum.Parse<RecoveryModel>(database.RecoveryModel);
            prototype.DatabaseCompatibilityLevel = displayCompatLevels[database.CompatibilityLevel];
            if (prototype is DatabasePrototype110 db110)
            {
                db110.DatabaseContainmentType = Enum.Parse<ContainmentType>(database.ContainmentType);
            }
            var action = updateExistingDB ? ConfigAction.Update : ConfigAction.Create;
            return ConfigureDatabase(dataContainer, ConfigAction.Create, runType, prototype);
        }

        private string ConfigureDatabase(CDataContainer dataContainer, ConfigAction configAction, RunType runType, DatabasePrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new DatabaseActions(dataContainer, configAction, prototype))
            {
                var executionHandler = new ExecutonHandler(actions);
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

        private DatabasePrototype CreateDatabasePrototype(CDataContainer context)
        {
            if (context == null)
            {
                throw new ArgumentException("Context not provided");
            }
            DatabasePrototype prototype = null;

            int majorVersionNumber = context.Server.Information.Version.Major;
            Version sql2000sp3 = new Version(8, 0, 760);
            Version sql2005sp2 = new Version(9, 0, 3000);
            if (context.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                prototype = new DatabasePrototypeAzure(context, DatabaseEngineEdition.SqlOnDemand);
            }
            else if (context.Server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                prototype = new DatabasePrototypeAzure(context);
            }
            else if (VersionUtils.IsSql16OrLater(context.Server.ServerVersion))
            {
                prototype = new DatabasePrototype160(context);
            }
            else if (VersionUtils.IsSql14OrLater(context.Server.ServerVersion))
            {
                prototype = new DatabasePrototype140(context);
            }
            else if (VersionUtils.IsSql11OrLater(context.Server.ServerVersion))
            {
                prototype = new DatabasePrototype110(context);
            }
            else if (majorVersionNumber == 10)
            {
                prototype = new DatabasePrototype100(context);
            }
            else if ((sql2005sp2 <= context.Server.Information.Version) &&
                (context.Server.Information.EngineEdition == Edition.EnterpriseOrDeveloper))
            {
                prototype = new DatabasePrototype90EnterpriseSP2(context);
            }
            else if (8 < majorVersionNumber)
            {
                prototype = new DatabasePrototype90(context);
            }
            else if (sql2000sp3 <= context.Server.Information.Version)
            {
                prototype = new DatabasePrototype80SP3(context);
            }
            else if (7 < majorVersionNumber)
            {
                prototype = new DatabasePrototype80(context);
            }
            else
            {
                prototype = new DatabasePrototype(context);
            }

            prototype.Initialize();
            return prototype;
        }

        /// <summary>
        /// Populate the collation dropdown
        /// </summary>
        private string[] PopulateCollationDropdown(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            var collationItems = new List<string>();
            bool isSphinxServer = (dataContainer.Server.VersionMajor < minimumVersionForWritableCollation);

            // if we're creating a new database or this is a Sphinx Server, add "<Default>" to the dropdown
            if (dataContainer.IsNewObject || isSphinxServer)
            {
                collationItems.Add(this.resourceManager.GetString("general.default"));
            }

            // if the server is shiloh or later, add specific collations to the dropdown
            if (!isSphinxServer)
            {
                DataTable serverCollationsTable = dataContainer.Server.EnumCollations();
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
                    collationItems.Prepend(prototype.Collation);
                }
            }
            return collationItems.ToArray();
        }

        /// <summary>
        /// Populate the collation dropdown with the prototype's collation
        /// </summary>
        private string[] PopulateCollationDropdownWithPrototypeCollation(DatabasePrototype prototype)
        {
            return new string[] { prototype.Collation };
        }

        /// <summary>
        /// Populates the containment type dropdown.
        /// </summary>
        private string[] PopulateContainmentTypeDropdown(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            if (!(SqlMgmtUtils.IsSql11OrLater(dataContainer.Server.ServerVersion)) || IsAnyManagedInstance(dataContainer.Server))
            {
                return null;
            }

            var containmentTypes = new List<string>();
            ContainmentType dbContainmentType = ContainmentType.None;
            DatabasePrototype110 dp110 = prototype as DatabasePrototype110;

            if (dp110 != null)
            {
                dbContainmentType = dp110.DatabaseContainmentType;
            }

            containmentTypes.Add(this.resourceManager.GetString("general.containmentType.None"));
            containmentTypes.Add(this.resourceManager.GetString("general.containmentType.Partial"));

            var swapIndex = 0;
            switch (dbContainmentType)
            {
                case ContainmentType.None:
                    break;
                case ContainmentType.Partial:
                    swapIndex = 1;
                    break;
                default:
                    System.Diagnostics.Debug.Fail("Unexpected Containment type.");
                    break;
            }
            if (swapIndex > 0)
            {
                var value = containmentTypes[swapIndex];
                containmentTypes.RemoveAt(swapIndex);
                containmentTypes.Prepend(value);
            }

            return containmentTypes.ToArray();
        }

        /// <summary>
        /// Populates the recovery model dropdown.
        /// </summary>
        private string[] PopulateRecoveryModelDropdown(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            // if the server is shiloh or later, but not Managed Instance, enable the dropdown
            var recoveryModelEnabled = (minimumVersionForRecoveryModel <= dataContainer.Server.VersionMajor) && !IsAnyManagedInstance(dataContainer.Server);
            if (dataContainer.Server.GetDisabledProperties().Contains("RecoveryModel") || !recoveryModelEnabled)
            {
                return null;
            }

            var recoveryModels = new List<string>();
            // Note: we still discriminate on IsAnyManagedInstance(dataContainer.Server) because GetDisabledProperties()
            //       was not updated to handle SQL Managed Instance. I suppose we could cleanup
            //       this code if we updated GetDisabledProperties() to handle MI as well.
            if (!IsAnyManagedInstance(dataContainer.Server))
            {
                // add recovery model options to the dropdown
                recoveryModels.Add(this.resourceManager.GetString("general.recoveryModel.full"));
                recoveryModels.Add(this.resourceManager.GetString("general.recoveryModel.bulkLogged"));
                recoveryModels.Add(this.resourceManager.GetString("general.recoveryModel.simple"));
            }
            else
            {
                if (prototype.OriginalName.Equals("tempdb", StringComparison.CurrentCultureIgnoreCase) && prototype.IsSystemDB)
                {
                    // tempdb supports 'simple recovery' only
                    recoveryModels.Add(this.resourceManager.GetString("general.recoveryModel.simple"));
                }
                else
                {
                    // non-tempdb supports only 'full recovery' model
                    recoveryModels.Add(this.resourceManager.GetString("general.recoveryModel.full"));
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
                        System.Diagnostics.Debug.Assert(RecoveryModel.Full == prototype.RecoveryModel, "unexpected recovery model");
                        break;
                }
                if (swapIndex > 0)
                {
                    var value = recoveryModels[swapIndex];
                    recoveryModels.RemoveAt(swapIndex);
                    recoveryModels.Prepend(value);
                }
            }
            return recoveryModels.ToArray();
        }

        private string[] PopulateCompatibilityLevelDropdownAzure(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            // For Azure we loop through all of the possible compatibility levels. We do this because there's only one compat level active on a
            // version at a time, but that can change at any point so in order to reduce maintenance required when that happens we'll just find
            // the one that matches the current set level and display that
            foreach (var level in this.compatLevels.Keys)
            {
                if (level == prototype.DatabaseCompatibilityLevel)
                {
                    // Azure can't change the compat level so we only populate the current version
                    return new string[] { this.compatLevels[level] };
                }
            }

            System.Diagnostics.Debug.Fail(string.Format(CultureInfo.InvariantCulture, "Unknown compat version '{0}'", prototype.DatabaseCompatibilityLevel));
            return null;
        }

        /// <summary>
        /// Populate the compatibility level dropdown and select the appropriate item
        /// </summary>
        private string[] PopulateCompatibilityLevelDropdown(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            // Unlikely that we are hitting such an old SQL Server, but leaving to preserve
            // the original semantic of this method.
            if (dataContainer.SqlServerVersion < 8)
            {
                // we do not know this version number, we do not know the possible compatibility levels for the server
                return null;
            }

            var compatibilityLevels = new List<string>();
            switch (dataContainer.SqlServerVersion)
            {
                case 8:     // Shiloh
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version70]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                    break;
                case 9:     // Yukon
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version70]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                    break;
                case 10:    // Katmai
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    break;
                case 11:    // Denali
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    break;
                case 12:    // SQL2014
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                    break;
                case 13:    // SQL2016
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                    break;
                case 14:    // SQL2017
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                    break;
                case 15:    // SQL2019
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version150]);
                    break;
                /* SQL_VBUMP_REVIEW */
                default:
                    // It is either the latest SQL we know about, or some future version of SQL we
                    // do not know about. We play conservative and only add the compat level we know
                    // about so far.
                    // At vBump, add a new case and move the 'default' label there.
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version150]);
                    compatibilityLevels.Add(this.compatLevels[CompatibilityLevel.Version160]);
                    break;
            }

            // set the compatability level for this combo box based on the prototype
            int swapIndex = 0;
            foreach (var level in this.compatLevels.Keys)
            {
                if (level == prototype.DatabaseCompatibilityLevel)
                {
                    if (swapIndex > 0)
                    {
                        var value = compatibilityLevels[swapIndex];
                        compatibilityLevels.RemoveAt(swapIndex);
                        compatibilityLevels.Prepend(value);
                    }
                    return compatibilityLevels.ToArray();
                }
                swapIndex++;
            }

            // previous loop did not find the prototype compatibility level in this server's compatability options
            // disable the compatability level option
            return null;
        }
    }
}