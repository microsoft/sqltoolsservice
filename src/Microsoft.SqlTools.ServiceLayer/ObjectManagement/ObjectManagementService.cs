//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
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
                return ConnectionService.Instance;
            }
            set
            {
                connectionService = value;
            }
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.serviceHost = serviceHost;
            this.serviceHost.SetRequestHandler(RenameRequest.Type, HandleRenameRequest);
        }

        /// <summary>
        /// Method to handle the renaming operation
        /// </summary>
        /// <param name="requestParams">parameters which are needed to execute renaming operation</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        internal async Task HandleRenameRequest(RenameRequestParams requestParams, RequestContext<bool> requestContext)
        {

            Logger.Verbose("Handle Request in HandleProcessRenameEditRequest()");
            ConnectionInfo connInfo;

            if (connectionService.TryFindConnection(
                    requestParams.ConnectionUri,
                    out connInfo))
            {
                using (SqlConnection sqlConn = ConnectionService.OpenSqlConnection(connInfo, ObjectManagementServiceApplicationName))
                {

                    IRenamable renameObject = this.GetSQLRenameObject(requestParams, sqlConn);

                    renameObject.Rename(requestParams.NewName);
                }
            }
            else
            {
                Logger.Error($"The connection {requestParams.ConnectionUri} could not be found.");
                throw new Exception(SR.ErrorConnectionNotFound);
            }

            await requestContext.SendResult(true);

        }

        /// <summary>
        /// Method to get the sql object, which should be renamed
        /// </summary>
        /// <param name="requestParams">parameters which are required for the rename operation</param>
        /// <param name="connection">the sqlconnection on the server to search for the sqlobject</param>
        /// <returns>the sql object if implements the interface IRenamable, so they can be renamed</returns>
        private IRenamable GetSQLRenameObject(RenameRequestParams requestParams, SqlConnection connection)
        {
            ServerConnection serverConnection = new ServerConnection(connection);
            Server server = new Server(serverConnection);
            SqlSmoObject dbObject = server.GetSmoObject(new Urn(requestParams.ObjectUrn));


            return (IRenamable)dbObject;
        }
    }
}