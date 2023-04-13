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
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal class DatabaseRoleServiceHandlerImpl
    {
        private Dictionary<string, string> contextIdToConnectionUriMap = new Dictionary<string, string>();

        private ConnectionService? connectionService;

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                connectionService ??= ConnectionService.Instance;
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Handle request to create a new database role
        /// </summary>
        internal async Task HandleCreateDatabaseRoleRequest(CreateDatabaseRoleParams parameters, RequestContext<object> requestContext)
        {
            ConnectionInfo connInfo;
            string ownerUri;
            contextIdToConnectionUriMap.TryGetValue(parameters.ContextId, out ownerUri);
            ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);

            if (connInfo == null) 
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            await requestContext.SendResult(new object());
        }

        internal async Task HandleUpdateDatabaseRoleRequest(UpdateDatabaseRoleParams parameters, RequestContext<object> requestContext)
        {
            ConnectionInfo connInfo;
            string ownerUri;
            contextIdToConnectionUriMap.TryGetValue(parameters.ContextId, out ownerUri);
            ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
            if (connInfo == null) 
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }

            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
            await requestContext.SendResult(new object());
        }

        internal async Task HandleInitializeDatabaseRoleViewRequest(InitializeDatabaseRoleViewRequestParams parameters, RequestContext<DatabaseRoleViewInfo> requestContext)
        {
            contextIdToConnectionUriMap.Add(parameters.ContextId, parameters.ConnectionUri);
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null) 
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            DatabaseRoleInfo databaseRole = new DatabaseRoleInfo();
            await requestContext.SendResult(new DatabaseRoleViewInfo()
            {
                DatabaseRole = databaseRole
            });
        }

        internal async Task HandleDisposeDatabaseRoleViewRequest(DisposeDatabaseRoleViewRequestParams parameters, RequestContext<object> requestContext)
        {
            await requestContext.SendResult(new object());
        }
    }
}
