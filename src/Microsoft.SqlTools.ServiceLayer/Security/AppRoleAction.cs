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
    internal class AppRoleServiceHandlerImpl
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
        /// Handle request to create a new app role
        /// </summary>
        internal async Task HandleCreateAppRoleRequest(CreateAppRoleParams parameters, RequestContext<object> requestContext)
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
            AppRoleGeneral appRoleGeneral = new AppRoleGeneral(dataContainer, parameters.AppRole, true);
            appRoleGeneral.SendDataToServer();
            await requestContext.SendResult(new object());
        }

        internal async Task HandleUpdateAppRoleRequest(UpdateAppRoleParams parameters, RequestContext<object> requestContext)
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
            AppRoleGeneral appRoleGeneral = new AppRoleGeneral(dataContainer, parameters.AppRole, false);
            appRoleGeneral.SendDataToServer();

            await requestContext.SendResult(new object());
        }

        internal async Task HandleInitializeAppRoleViewRequest(InitializeAppRoleViewRequestParams parameters, RequestContext<AppRoleViewInfo> requestContext)
        {
            contextIdToConnectionUriMap.Add(parameters.ContextId, parameters.ConnectionUri);
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(parameters.ConnectionUri, out connInfo);
            if (connInfo == null) 
            {
                throw new ArgumentException("Invalid ConnectionUri");
            }
            CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            AppRoleInfo appRole = new AppRoleInfo();
            await requestContext.SendResult(new AppRoleViewInfo()
            {
                AppRole = appRole
            });
        }

        internal async Task HandleDisposeAppRoleViewRequest(DisposeAppRoleViewRequestParams parameters, RequestContext<object> requestContext)
        {
            await requestContext.SendResult(new object());
        }
    }
}
