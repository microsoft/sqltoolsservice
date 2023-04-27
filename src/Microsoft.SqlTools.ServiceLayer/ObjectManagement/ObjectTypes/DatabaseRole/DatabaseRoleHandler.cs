//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// DatabaseRole object type handler
    /// </summary>
    public class DatabaseRoleHandler : ObjectTypeHandler<DatabaseRoleInfo, DatabaseRoleViewContext>
    {
        public DatabaseRoleHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.DatabaseRole;
        }

        public override async Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            // check input parameters
            if (string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentNullException("parameters.Database");
            }

            // open a connection for running the DatabaseRole associated tasks
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", parameters.ConnectionUri);
            }
            string originalDatabaseName = originalConnInfo.ConnectionDetails.DatabaseName;
            try
            {
                originalConnInfo.ConnectionDetails.DatabaseName = parameters.Database;
                ConnectParams connectParams = new ConnectParams
                {
                    OwnerUri = parameters.ContextId,
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
            this.ConnectionService.TryFindConnection(parameters.ContextId, out connInfo);

            CDataContainer dataContainer = CreateDatabaseRoleDataContainer(connInfo, null, ConfigAction.Create, parameters.Database);

            DatabaseRolePrototype prototype = parameters.IsNewObject
            ? new DatabaseRolePrototype(dataContainer, parameters.Database)
            : new DatabaseRolePrototype(dataContainer, parameters.Database, dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as DatabaseRole);

            DatabaseRoleInfo DatabaseRoleInfo = new DatabaseRoleInfo()
            {
                Name = prototype.Name,
                Owner = prototype.Owner,
                ExtendedProperties = prototype.ExtendedProperties.Select(item => new ExtendedPropertyInfo()
                {
                    Name = item.Key,
                    Value = item.Value
                }).ToArray(),
                Members = prototype.Members.ToArray(),
                OwnedSchemas = prototype.SchemasOwned.ToArray(),
            };

            var viewInfo = new DatabaseRoleViewInfo()
            {
                ObjectInfo = DatabaseRoleInfo,
                Schemas = prototype.Schemas
            };

            var context = new DatabaseRoleViewContext(parameters, dataContainer.ServerConnection);
            return new InitializeViewResult()
            {
                ViewInfo = viewInfo,
                Context = context
            };
        }

        public override Task Save(DatabaseRoleViewContext context, DatabaseRoleInfo obj)
        {
            if (context.Parameters.IsNewObject)
            {
                this.DoHandleCreateDatabaseRoleRequest(context, obj, RunType.RunNow);
            }
            else
            {
                this.DoHandleUpdateDatabaseRoleRequest(context, obj, RunType.RunNow);
            }
            return Task.CompletedTask;
        }

        public override Task<string> Script(DatabaseRoleViewContext context, DatabaseRoleInfo obj)
        {
            string script;
            if (context.Parameters.IsNewObject)
            {
                script = this.DoHandleCreateDatabaseRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            else
            {
                script = this.DoHandleUpdateDatabaseRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            return Task.FromResult(script);
        }

        private string ConfigureDatabaseRole(CDataContainer dataContainer, ConfigAction configAction, RunType runType, DatabaseRolePrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new DatabaseRoleActions(dataContainer, configAction, prototype))
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

        private string DoHandleUpdateDatabaseRoleRequest(DatabaseRoleViewContext context, DatabaseRoleInfo DatabaseRoleInfo, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CreateDatabaseRoleDataContainer(connInfo, null, ConfigAction.Create, context.Parameters.Database);
            DatabaseRolePrototype prototype = new DatabaseRolePrototype(dataContainer, context.Parameters.Database, dataContainer.Server.Databases[context.Parameters.Database].Roles[DatabaseRoleInfo.Name]);
            prototype.ApplyInfoToPrototype(DatabaseRoleInfo);
            return ConfigureDatabaseRole(dataContainer, ConfigAction.Update, runType, prototype);
        }

        private string DoHandleCreateDatabaseRoleRequest(DatabaseRoleViewContext context, DatabaseRoleInfo DatabaseRoleInfo, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CreateDatabaseRoleDataContainer(connInfo, null, ConfigAction.Create, context.Parameters.Database);

            DatabaseRolePrototype prototype = new DatabaseRolePrototype(dataContainer, context.Parameters.Database, DatabaseRoleInfo);
            return ConfigureDatabaseRole(dataContainer, ConfigAction.Create, runType, prototype);
        }

        internal CDataContainer CreateDatabaseRoleDataContainer(ConnectionInfo connInfo, DatabaseRoleInfo role, ConfigAction configAction, string databaseName)
        {
            var serverConnection = ConnectionService.OpenServerConnection(connInfo, "DataContainer");
            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;

            string urn = (configAction == ConfigAction.Update && role != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']/Role[@Name='{1}']",
                    Urn.EscapeString(databaseName),
                    Urn.EscapeString(role.Name))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName));

            ActionContext context = new ActionContext(serverConnection, "Role", urn);
            DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);

            if (configAction == ConfigAction.Create)
            {
                containerXml.AddProperty("itemtype", "Role");
            }

            XmlDocument xmlDoc = containerXml.GenerateXmlDocument();
            return CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);
        }
    }
}