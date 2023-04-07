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
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// Main class for ObjectManagement Service functionality
    /// </summary>
    public class ObjectManagementService
    {
        private const string ObjectManagementServiceApplicationName = "azdata-object-management";
        private static Lazy<ObjectManagementService> objectManagementServiceInstance = new Lazy<ObjectManagementService>(() => new ObjectManagementService());
        public static ObjectManagementService Instance => objectManagementServiceInstance.Value;
        public static ConnectionService connectionService;
        private IProtocolEndpoint serviceHost;
        public ObjectManagementService() { }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal static ConnectionService ConnectionServiceInstance
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

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.serviceHost = serviceHost;
            this.serviceHost.SetRequestHandler(RenameRequest.Type, HandleRenameRequest, true);
            this.serviceHost.SetRequestHandler(DropRequest.Type, HandleDropRequest, true);
        }

        /// <summary>
        /// Method to handle the renaming operation
        /// </summary>
        /// <param name="requestParams">parameters which are needed to execute renaming operation</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        internal async Task HandleRenameRequest(RenameRequestParams requestParams, RequestContext<bool> requestContext)
        {
            Logger.Verbose("Handle Request in HandleRenameRequest()");
            ExecuteActionOnObject(requestParams.ConnectionUri, requestParams.ObjectUrn, (dbObject) =>
            {
                var renamable = dbObject as IRenamable;
                if (renamable != null)
                {
                    renamable.Rename(requestParams.NewName);
                }
                else
                {
                    throw new Exception(SR.ObjectNotRenamable(requestParams.ObjectUrn));
                }
            });
            await requestContext.SendResult(true);
        }

        /// <summary>
        /// Method to handle the delete object request
        /// </summary>
        /// <param name="requestParams">parameters which are needed to execute deletion operation</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        internal async Task HandleDropRequest(DropRequestParams requestParams, RequestContext<bool> requestContext)
        {
            Logger.Verbose("Handle Request in HandleDeleteRequest()");
            ConnectionInfo connectionInfo = this.GetConnectionInfo(requestParams.ConnectionUri);
            using (CDataContainer dataContainer = CDataContainer.CreateDataContainer(connectionInfo, databaseExists: true))
            {
                try
                {
                    dataContainer.SqlDialogSubject = dataContainer.Server?.GetSmoObject(requestParams.ObjectUrn);
                    DatabaseUtils.DoDropObject(dataContainer);
                }
                catch (FailedOperationException ex)
                {
                    if (!(ex.InnerException is MissingObjectException) || (ex.InnerException is MissingObjectException && requestParams.ThrowIfNotExist))
                    {
                        throw;
                    }
                }
            }
            await requestContext.SendResult(true);
        }

        private ConnectionInfo GetConnectionInfo(string connectionUri)
        {
            ConnectionInfo connInfo;
            if (ConnectionServiceInstance.TryFindConnection(connectionUri, out connInfo))
            {
                return connInfo;
            }
            else
            {
                Logger.Error($"The connection with URI '{connectionUri}' could not be found.");
                throw new Exception(SR.ErrorConnectionNotFound);
            }
        }

        private void ExecuteActionOnObject(string connectionUri, string objectUrn, Action<SqlSmoObject> action)
        {
            ConnectionInfo connInfo = this.GetConnectionInfo(connectionUri);
            ServerConnection serverConnection = ConnectionService.OpenServerConnection(connInfo, ObjectManagementServiceApplicationName);
            using (serverConnection.SqlConnectionObject)
            {
                Server server = new Server(serverConnection);
                SqlSmoObject dbObject = server.GetSmoObject(new Urn(objectUrn));
                action(dbObject);
            }
        }
    }
}