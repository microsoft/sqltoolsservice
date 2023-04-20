//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

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
            DatabaseRoleViewInfo DatabaseRoleViewInfo = new DatabaseRoleViewInfo();

            DatabaseRoleInfo DatabaseRoleInfo = new DatabaseRoleInfo()
            {
                Name = "test",
            };

            var viewInfo = new DatabaseRoleViewInfo()
            {
                ObjectInfo = DatabaseRoleInfo,
            };

            var context = new DatabaseRoleViewContext(parameters);
            return Task.FromResult(new InitializeViewResult()
            {
                ViewInfo = viewInfo,
                Context = context
            });
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

        private string ConfigureDatabaseRole(CDataContainer dataContainer, ConfigAction configAction, RunType runType, LoginPrototype prototype)
        {
            string sqlScript = string.Empty;
            using (var actions = new LoginActions(dataContainer, configAction, prototype))
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

        private string DoHandleUpdateDatabaseRoleRequest(DatabaseRoleViewContext context, DatabaseRoleInfo DatabaseRole, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);
            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            return ConfigureDatabaseRole(dataContainer, ConfigAction.Update, runType, null);

        }

        private string DoHandleCreateDatabaseRoleRequest(DatabaseRoleViewContext context, DatabaseRoleInfo DatabaseRole, RunType runType)
        {
            ConnectionInfo connInfo;
            this.ConnectionService.TryFindConnection(context.Parameters.ConnectionUri, out connInfo);

            if (connInfo == null)
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);


            return ConfigureDatabaseRole(dataContainer, ConfigAction.Create, runType, null);
        }

    }
}