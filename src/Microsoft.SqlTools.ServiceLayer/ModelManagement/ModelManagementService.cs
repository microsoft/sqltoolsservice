//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{
    public class ModelManagementService
    {
        private ConnectionService connectionService = null;
        private static readonly Lazy<ModelManagementService> instance = new Lazy<ModelManagementService>(() => new ModelManagementService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ModelManagementService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ModelListRequest.Type, this.HandleModelListRequest);
            serviceHost.SetRequestHandler(ModelDeleteRequest.Type, this.HandleModelDeleteRequest);
            serviceHost.SetRequestHandler(ModelUpdateRequest.Type, this.HandleModelUpdateRequest);
        }

        /// <summary>
        /// Handles external language delete request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleModelDeleteRequest(ModelDeleteRequestParams parameters, RequestContext<ModelDeleteResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleModelDeleteRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ModelDeleteResponseParams response = new ModelDeleteResponseParams
                {
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        ModelManagementOperations serviceOperations = new ModelManagementOperations(parameters.DatabaseName, parameters.TableName);
                        serviceOperations.DeleteModel(dbConnection, parameters.Name);
                    }

                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles external language delete request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleModelUpdateRequest(ModelUpdateRequestParams parameters, RequestContext<ModelUpdateResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleModelUpdateRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ModelUpdateResponseParams response = new ModelUpdateResponseParams
                {
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        ModelManagementOperations serviceOperations = new ModelManagementOperations(parameters.DatabaseName, parameters.TableName);
                        serviceOperations.UpdateModel(dbConnection, parameters.Model);
                    }

                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }

        /// <summary>
        /// Handles external language status request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleModelListRequest(ModelListRequestParams parameters, RequestContext<ModelListResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleModelListRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ModelListResponseParams response = new ModelListResponseParams
                {
                };

                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        ModelManagementOperations serviceOperations = new ModelManagementOperations(parameters.DatabaseName, parameters.TableName);
                        response.Models = serviceOperations.GetModels(dbConnection);
                    }

                    await requestContext.SendResult(response);
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                await requestContext.SendError(e);
            }
        }
    }
}
