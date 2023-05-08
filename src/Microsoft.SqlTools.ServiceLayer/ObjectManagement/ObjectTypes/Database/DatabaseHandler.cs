//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
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
        private const string DefaultValue = "<default>";

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

        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams requestParams)
        {
            // open a connection for running the database dialog and associated task
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(requestParams.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", requestParams.ConnectionUri);
            }

            // create a default data context and database object
            ServerConnection serverConnection = ConnectionService.OpenServerConnection(originalConnInfo, "DataContainer");
            CDataContainer dataContainer = CreateDatabaseDataContainer(requestParams.ConnectionUri, ConfigAction.Create);

            var prototype = new DatabaseTaskHelper(dataContainer).Prototype;
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

            // Skip adding logins for the Owner field if running against an Azure SQL DB
            if (dataContainer.Server.ServerType != DatabaseEngineType.SqlAzureDatabase)
            {
                var logins = new List<string>();
                logins.Add(DefaultValue);
                foreach (Login login in dataContainer.Server.Logins)
                {
                    logins.Add(login.Name);
                }
                databaseViewInfo.LoginNames = logins.ToArray();
            }

            var context = new DatabaseViewContext(requestParams);
            return Task.FromResult(new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context });
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

        private CDataContainer CreateDatabaseDataContainer(string connectionUri, ConfigAction configAction, DatabaseInfo database = null)
        {
            ConnectionInfo connectionInfo = this.GetConnectionInfo(connectionUri);
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: configAction != ConfigAction.Create);
            string objectUrn = (configAction != ConfigAction.Create && database != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(database.Name))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server[@Name='{0}']",
                    Urn.EscapeString(dataContainer.ServerName));
            dataContainer.SqlDialogSubject = dataContainer.Server?.GetSmoObject(objectUrn);
            return dataContainer;
        }

        private string ConfigureDatabase(string connectionUri, DatabaseInfo database, ConfigAction configAction, RunType runType)
        {
            if (database.Name == null)
            {
                throw new ArgumentException("Database name not provided");
            }

            CDataContainer dataContainer = CreateDatabaseDataContainer(connectionUri, configAction, database);
            DatabasePrototype prototype = new DatabaseTaskHelper(dataContainer).Prototype;
            prototype.Name = database.Name;
            if (database.Owner != null && database.Owner != DefaultValue)
            {
                prototype.Owner = database.Owner;
            }
            if (database.CollationName != null && database.CollationName != DefaultValue)
            {
                prototype.Collation = database.CollationName;
            }
            if (database.RecoveryModel != null)
            {
                prototype.RecoveryModel = Enum.Parse<RecoveryModel>(database.RecoveryModel);
            }
            if (database.CompatibilityLevel != null)
            {
                prototype.DatabaseCompatibilityLevel = displayCompatLevels[database.CompatibilityLevel];
            }
            if (prototype is DatabasePrototype110 db110 && database.ContainmentType != null)
            {
                db110.DatabaseContainmentType = Enum.Parse<ContainmentType>(database.ContainmentType);
            }

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

        /// <summary>
        /// Populate the collation dropdown
        /// </summary>
        private string[] PopulateCollationDropdown(CDataContainer dataContainer, DatabasePrototype prototype)
        {
            var collationItems = new List<string>();
            bool isSphinxServer = (dataContainer.Server.VersionMajor < minimumVersionForWritableCollation);

            // if we're creating a new database or this is a Sphinx Server, add "<default>" to the dropdown
            if (dataContainer.IsNewObject || isSphinxServer)
            {
                collationItems.Add(DefaultValue);
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
                    collationItems.Insert(0, prototype.Collation);
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

            containmentTypes.Add(ContainmentType.None.ToString());
            containmentTypes.Add(ContainmentType.Partial.ToString());

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
                containmentTypes.Insert(0, value);
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
                recoveryModels.Add(RecoveryModel.Full.ToString());
                recoveryModels.Add(RecoveryModel.BulkLogged.ToString());
                recoveryModels.Add(RecoveryModel.Simple.ToString());
            }
            else
            {
                if (prototype.OriginalName.Equals("tempdb", StringComparison.CurrentCultureIgnoreCase) && prototype.IsSystemDB)
                {
                    // tempdb supports 'simple recovery' only
                    recoveryModels.Add(RecoveryModel.Simple.ToString());
                }
                else
                {
                    // non-tempdb supports only 'full recovery' model
                    recoveryModels.Add(RecoveryModel.Full.ToString());
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
                    recoveryModels.Insert(0, value);
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
            for (var i = 0; i < compatibilityLevels.Count; i++)
            {
                var level = compatibilityLevels[i];
                var prototypeLevel = this.compatLevels[prototype.DatabaseCompatibilityLevel];
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

            // previous loop did not find the prototype compatibility level in this server's compatability options
            // disable the compatability level option
            return null;
        }
    }
}