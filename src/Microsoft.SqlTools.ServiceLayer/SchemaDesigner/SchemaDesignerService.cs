//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerService : HostedService<SchemaDesignerService>, IComposableService, IHostedService, IDisposable
    {
        private IProtocolEndpoint serviceHost;
        private ConnectedBindingQueue bindingQueue = new ConnectedBindingQueue(needsMetadata: false);
        private ConnectionService connectionService;
        private IMultiServiceProvider serviceProvider;
        private string connectionName = "SchemaDesigner";


        public SchemaDesignerService()
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// As an <see cref="IComposableService"/>, this will be set whenever the service is initialized
        /// via an <see cref="IMultiServiceProvider"/>
        /// </summary>
        /// <param name="provider"></param>
        public override void SetServiceProvider(IMultiServiceProvider provider)
        {
            Validate.IsNotNull(nameof(provider), provider);
            serviceProvider = provider;
            connectionService = provider.GetService<ConnectionService>();
            try
            {
                connectionService.RegisterConnectedQueue(connectionName, bindingQueue);

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Verbose("Initializing Schema Designer Service");
            this.serviceHost = serviceHost;

            serviceHost.SetRequestHandler(GetSchemaModelRequest.Type, HandleGetSchemaModelRequest);
        }


        internal async Task HandleGetSchemaModelRequest(ConnectionDetails connectionDetails, RequestContext<SchemaModel> requestContext)
        {
            try
            {
                var uri = ConnectedBindingQueue.GetConnectionContextKey(connectionDetails);
                ConnectParams connectParams = new ConnectParams() { OwnerUri = uri, Connection = connectionDetails, Type = Connection.ConnectionType.ObjectExplorer };
                bool isDefaultOrSystemDatabase = DatabaseUtils.IsSystemDatabaseConnection(connectionDetails.DatabaseName) || string.IsNullOrWhiteSpace(connectionDetails.DatabaseDisplayName);

                ConnectionInfo connectionInfo;
                ConnectionCompleteParams connectionResult = await Connect(connectParams, uri);
            }
            catch (Exception e)
            {
                await requestContext.SendError(e.ToString());
            }
        }

        private async Task<ConnectionCompleteParams> Connect(ConnectParams connectParams, string uri)
        {
            string connectionErrorMessage = string.Empty;
            try
            {
                // open connection based on request details
                ConnectionCompleteParams result = await connectionService.Connect(connectParams);
                connectionErrorMessage = result != null ? $"{result.Messages} error code:{result.ErrorNumber}" : string.Empty;
                if (result != null && !string.IsNullOrEmpty(result.ConnectionId))
                {
                    return result;
                }
                else
                {
                    throw new Exception(connectionErrorMessage);
                }

            }
            catch (Exception ex)
            {
                int? errorNum = ex is SqlException sqlEx ? sqlEx.Number : null;
                return null;
            }
        }

        /// <summary>
        /// Generates a URI for object explorer using a similar pattern to Mongo DB (which has URI-based database definition)
        /// as this should ensure uniqueness
        /// </summary>
        /// <param name="details"></param>
        /// <returns>string representing a URI</returns>
        /// <remarks>Internal for testing purposes only</remarks>
        internal static string GenerateUri(ConnectionDetails details)
        {
            return ConnectedBindingQueue.GetConnectionContextKey(details);
        }

    }
}