//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// ServerRole object type handler
    /// </summary>
    public class ServerRoleHandler : ObjectTypeHandler<ServerRoleInfo, ServerRoleViewContext>
    {
        public ServerRoleHandler(ConnectionService connectionService) : base(connectionService) { }

        public override bool CanHandleType(SqlObjectType objectType)
        {
            return objectType == SqlObjectType.ApplicationRole;
        }

        public override Task<InitializeViewResult> InitializeObjectView(InitializeViewRequestParams parameters)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            ServerRolePrototype prototype = parameters.IsNewObject
                ? new ServerRolePrototype(dataContainer)
                : new ServerRolePrototype(dataContainer, dataContainer.Server.GetSmoObject(parameters.ObjectUrn) as ServerRole);

            ServerRoleInfo ServerRoleInfo = new ServerRoleInfo()
            {
                Name = prototype.Name,
                Owner = prototype.Owner,
                Members = prototype.Members.ToArray(),
                Memberships = prototype.Memberships.ToArray(),
                IsFixedRole = prototype.IsFixedRole
            };

            var viewInfo = new ServerRoleViewInfo()
            {
                ObjectInfo = ServerRoleInfo,
            };

            var context = new ServerRoleViewContext(parameters);
            return Task.FromResult(new InitializeViewResult()
            {
                ViewInfo = viewInfo,
                Context = context
            });
        }

        public override Task Save(ServerRoleViewContext context, ServerRoleInfo obj)
        {
            if (context.Parameters.IsNewObject)
            {
                this.DoHandleCreateServerRoleRequest(context, obj, RunType.RunNow);
            }
            else
            {
                this.DoHandleUpdateServerRoleRequest(context, obj, RunType.RunNow);
            }
            return Task.CompletedTask;
        }

        public override Task<string> Script(ServerRoleViewContext context, ServerRoleInfo obj)
        {
            string script;
            if (context.Parameters.IsNewObject)
            {
                script = this.DoHandleCreateServerRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            else
            {
                script = this.DoHandleUpdateServerRoleRequest(context, obj, RunType.ScriptToWindow);
            }
            return Task.FromResult(script);
        }

        private string ConfigureServerRole(CDataContainer dataContainer, ConfigAction configAction, RunType runType, ServerRolePrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new ServerRoleActions(dataContainer, configAction, prototype))
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

        private string DoHandleUpdateServerRoleRequest(ServerRoleViewContext context, ServerRoleInfo serverRoleInfo, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            ServerRolePrototype prototype = new ServerRolePrototype(dataContainer, dataContainer.Server.Roles[serverRoleInfo.Name]);
            prototype.ApplyInfoToPrototype(serverRoleInfo);
            return ConfigureServerRole(dataContainer, ConfigAction.Update, runType, null);

        }

        private string DoHandleCreateServerRoleRequest(ServerRoleViewContext context, ServerRoleInfo serverRoleInfo, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            ServerRolePrototype prototype = new ServerRolePrototype(dataContainer, serverRoleInfo);
            return ConfigureServerRole(dataContainer, ConfigAction.Create, runType, null);
        }
    }
}
