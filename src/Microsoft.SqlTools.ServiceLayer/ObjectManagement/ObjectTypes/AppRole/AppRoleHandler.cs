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
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// AppRole object type handler
    /// </summary>
    public class AppRoleHandler : ObjectTypeHandler<AppRoleInfo, AppRoleViewContext>
    {
        public AppRoleHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.ApplicationRole;
        }

        public override async Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            // check input parameters
            if (string.IsNullOrWhiteSpace(parameters.Database))
            {
                throw new ArgumentNullException("parameters.Database");
            }

            // open a connection for running the AppRole associated tasks
            ConnectionInfo originalConnInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out originalConnInfo);
            if (originalConnInfo == null)
            {
                throw new ArgumentException("Invalid connection URI '{0}'", parameters.ConnectionUri);
            }
            string originalDatabaseName = originalConnInfo.ConnectionDetails.DatabaseName;
            originalConnInfo.ConnectionDetails.DatabaseName = parameters.Database;

            // create a default app role data context and database object
            CDataContainer dataContainer;
            try
            {
                ServerConnection serverConnection = ConnectionService.OpenServerConnection(originalConnInfo, "DataContainer");
                dataContainer = CreateAppRoleDataContainer(serverConnection, null, ConfigAction.Create, parameters.Database);
            }
            finally
            {
                originalConnInfo.ConnectionDetails.DatabaseName = originalDatabaseName;
            }

            AppRolePrototype prototype = parameters.IsNewObject
            ? new AppRolePrototype(dataContainer, parameters.Database)
            : new AppRolePrototype(dataContainer, parameters.Database, dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as ApplicationRole);

            AppRoleInfo appRoleInfo = new AppRoleInfo()
            {
                Name = prototype.Name,
                DefaultSchema = prototype.DefaultSchema,
                Password = prototype.Password,
                ExtendedProperties = prototype.ExtendedProperties.Select(
                    item => new ExtendedPropertyInfo()
                    {
                        Name = item.Key,
                        Value = item.Value
                    }).ToArray(),
                OwnedSchemas = prototype.SchemasOwned,
                SecurablePermissions = prototype.SecurablePermissions
            };

            var viewInfo = new AppRoleViewInfo()
            {
                ObjectInfo = appRoleInfo,
                Schemas = prototype.Schemas,
                SupportedSecurableTypes = SecurableUtils.GetSecurableTypeMetadata(SqlObjectType.ApplicationRole, dataContainer.Server.Version, parameters.Database, dataContainer.Server.DatabaseEngineType, dataContainer.Server.DatabaseEngineEdition)
            };

            var context = new AppRoleViewContext(parameters, dataContainer.ServerConnection);
            return new InitializeViewResult()
            {
                ViewInfo = viewInfo,
                Context = context
            };
        }

        public override Task Save(AppRoleViewContext context, AppRoleInfo obj)
        {
            if (context.Parameters.IsNewObject)
            {
                this.DoHandleCreateAppRoleRequest(context, obj, RunType.RunNow);
            }
            else
            {
                this.DoHandleUpdateAppRoleRequest(context, obj, RunType.RunNow);
            }
            return Task.CompletedTask;
        }

        public override Task<string> Script(AppRoleViewContext context, AppRoleInfo obj)
        {
            string script;
            if (context.Parameters.IsNewObject)
            {
                script = this.DoHandleCreateAppRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            else
            {
                script = this.DoHandleUpdateAppRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            return Task.FromResult(script);
        }

        private string ConfigureAppRole(CDataContainer dataContainer, ConfigAction configAction, RunType runType, AppRolePrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new AppRoleActions(dataContainer, configAction, prototype))
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

        private string DoHandleUpdateAppRoleRequest(AppRoleViewContext context, AppRoleInfo appRoleInfo, RunType runType)
        {
            CDataContainer dataContainer = CreateAppRoleDataContainer(context.Connection, null, ConfigAction.Create, context.Parameters.Database);
            AppRolePrototype prototype = new AppRolePrototype(dataContainer, context.Parameters.Database, dataContainer.Server.Databases[context.Parameters.Database].ApplicationRoles[appRoleInfo.Name]);
            prototype.ApplyInfoToPrototype(appRoleInfo);
            return ConfigureAppRole(dataContainer, ConfigAction.Update, runType, prototype);
        }

        private string DoHandleCreateAppRoleRequest(AppRoleViewContext context, AppRoleInfo appRoleInfo, RunType runType)
        {
            CDataContainer dataContainer = CreateAppRoleDataContainer(context.Connection, null, ConfigAction.Create, context.Parameters.Database);

            AppRolePrototype prototype = new AppRolePrototype(dataContainer, context.Parameters.Database, appRoleInfo);
            return ConfigureAppRole(dataContainer, ConfigAction.Create, runType, prototype);
        }

        internal CDataContainer CreateAppRoleDataContainer(ServerConnection serverConnection, AppRoleInfo role, ConfigAction configAction, string databaseName)
        {
            var connectionInfoWithConnection = new SqlConnectionInfoWithConnection();
            connectionInfoWithConnection.ServerConnection = serverConnection;

            string urn = (configAction == ConfigAction.Update && role != null)
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']/ApplicationRole[@Name='{1}']",
                    Urn.EscapeString(databaseName),
                    Urn.EscapeString(role.Name))
                : string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Server/Database[@Name='{0}']",
                    Urn.EscapeString(databaseName));

            ActionContext context = new ActionContext(serverConnection, "ApplicationRole", urn);
            DataContainerXmlGenerator containerXml = new DataContainerXmlGenerator(context);

            if (configAction == ConfigAction.Create)
            {
                containerXml.AddProperty("itemtype", "ApplicationRole");
            }

            XmlDocument xmlDoc = containerXml.GenerateXmlDocument();
            return CDataContainer.CreateDataContainer(connectionInfoWithConnection, xmlDoc);
        }
    }
}