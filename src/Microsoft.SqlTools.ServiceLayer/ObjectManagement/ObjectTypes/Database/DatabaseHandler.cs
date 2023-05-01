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

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Login object type handler
    /// </summary>
    public class DatabaseHandler : ObjectTypeHandler<DatabaseInfo, DatabaseViewContext>
    {
        private const int minimumVersionForWritableCollation = 8;
        private const int minimumVersionForRecoveryModel = 8;

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

        public DatabaseHandler(ConnectionService connectionService) : base(connectionService) { }

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

            var databaseViewInfo = new DatabaseViewInfo()
            {
                ObjectInfo = new DatabaseInfo()
                {
                    IsAzure = parentServer.ServerType == DatabaseEngineType.SqlAzureDatabase
                }
            };

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

            databaseViewInfo.CollationNames = this.GetCollations(parentServer);
            databaseViewInfo.CompatibilityLevels = this.GetCompatibilityLevels(parentServer);
            databaseViewInfo.ContainmentTypes = this.GetContainmentTypes(parentServer);
            databaseViewInfo.RecoveryModels = this.GetRecoveryModels(parentServer);

            var context = new DatabaseViewContext(requestParams);
            return new InitializeViewResult { ViewInfo = databaseViewInfo, Context = context };
        }

        private string[] GetCollations(Server server)
        {
            var collations = new List<string>();
            bool isSphinxServer = server.VersionMajor < minimumVersionForWritableCollation;

            // if the server is shiloh or later, add specific collations to the dropdown
            if (!isSphinxServer)
            {
                var collationsTable = server.EnumCollations();
                if (collationsTable != null)
                {
                    foreach (DataRow serverCollation in collationsTable.Rows)
                    {
                        string collationName = (string)serverCollation["Name"];
                        collations.Add(collationName);
                    }
                }
            }
            return collations.ToArray();
        }

        private string[] GetCompatibilityLevels(Server server)
        {
            var levels = new List<string>();
            if (server.VersionMajor >= 8)
            {
                switch (server.VersionMajor)
                {
                    case 8:     // Shiloh
                        levels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version70]);
                        break;
                    case 9:     // Yukon
                        levels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version70]);
                        break;
                    case 10:    // Katmai
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version80]);
                        break;
                    case 11:    // Denali
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version90]);
                        break;
                    case 12:    // SQL2014
                        levels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        break;
                    case 13:    // SQL2016
                        levels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        break;
                    case 14:    // SQL2017
                        levels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        break;
                    case 15:    // SQL2019
                        levels.Add(this.compatLevels[CompatibilityLevel.Version150]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        break;
                    default:
                        // It is either the latest SQL we know about, or some future version of SQL we
                        // do not know about. We play conservative and only add the compat level we know
                        // about so far.
                        levels.Add(this.compatLevels[CompatibilityLevel.Version160]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version150]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version140]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version130]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version120]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version110]);
                        levels.Add(this.compatLevels[CompatibilityLevel.Version100]);
                        break;
                }
            }
            return levels.ToArray();
        }

        private string[] GetContainmentTypes(Server server)
        {
            var types = new List<string>();
            if (server.VersionMajor >= 11 && !IsAnyManagedInstance(server))
            {
                types.Add(ContainmentType.None.ToString());
                types.Add(ContainmentType.Partial.ToString());
            }
            return types.ToArray();
        }

        private string[] GetRecoveryModels(Server server)
        {
            var models = new List<string>();
            if (!server.GetDisabledProperties().Contains("RecoveryModel") && (server.VersionMajor >= minimumVersionForRecoveryModel) && !IsAnyManagedInstance(server))
            {
                if (!IsAnyManagedInstance(server))
                {
                    models.Add(RecoveryModel.Simple.ToString());
                    models.Add(RecoveryModel.BulkLogged.ToString());
                }
                models.Add(RecoveryModel.Full.ToString());
            }
            return models.ToArray();
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
            DatabasePrototype prototype = new DatabasePrototype(dataContainer);

            prototype.Name = database.Name;
            prototype.Owner = database.Owner;
            prototype.Collation = database.CollationName;
            prototype.RecoveryModel = Enum.Parse<RecoveryModel>(database.RecoveryModel);
            prototype.DatabaseCompatibilityLevel = displayCompatLevels[database.CompatibilityLevel];
            if (prototype is DatabasePrototype110 db110) {
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
    }
}