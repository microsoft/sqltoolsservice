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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{
    public class ModelManagementService
    {
        private ModelOperations serviceOperations = new ModelOperations();
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
                connectionService ??= ConnectionService.Instance;
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        public ModelOperations ModelOperations
        {
            get
            {
                return serviceOperations;
            }
            set
            {
                serviceOperations = value;
            }
        }

        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ImportModelRequest.Type, this.HandleModelImportRequest, true);
            serviceHost.SetRequestHandler(ConfigureModelTableRequest.Type, this.HandleConfigureModelTableRequest, true);
            serviceHost.SetRequestHandler(DeleteModelRequest.Type, this.HandleDeleteModelRequest, true);
            serviceHost.SetRequestHandler(DownloadModelRequest.Type, this.HandleDownloadModelRequest, true);
            serviceHost.SetRequestHandler(GetModelsRequest.Type, this.HandleGetModelsRequest, true);
            serviceHost.SetRequestHandler(UpdateModelRequest.Type, this.HandleUpdateModelRequest, true);
            serviceHost.SetRequestHandler(VerifyModelTableRequest.Type, this.HandleVerifyModelTableRequest, true);
        }

        /// <summary>
        /// Handles import model request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleModelImportRequest(ImportModelRequestParams parameters, RequestContext<ImportModelResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleModelImportRequest");
                ImportModelResponseParams response = new ImportModelResponseParams
                {
                };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                ModelOperations.ImportModel(dbConnection, parameters);
                return response;
            });
        }

        /// <summary>
        /// Handles get models request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleGetModelsRequest(GetModelsRequestParams parameters, RequestContext<GetModelsResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleGetModelsRequest");
            GetModelsResponseParams response = new GetModelsResponseParams
            {
            };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                List<ModelMetadata> models = ModelOperations.GetModels(dbConnection, parameters);
                response.Models = models;
                return response;
            });
        }

        /// <summary>
        /// Handles update model request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleUpdateModelRequest(UpdateModelRequestParams parameters, RequestContext<UpdateModelResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleUpdateModelRequest");
            UpdateModelResponseParams response = new UpdateModelResponseParams
            {
            };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                ModelOperations.UpdateModel(dbConnection, parameters);
                return response;
            });
        }

        /// <summary>
        /// Handles delete model request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleDeleteModelRequest(DeleteModelRequestParams parameters, RequestContext<DeleteModelResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleDeleteModelRequest");
            DeleteModelResponseParams response = new DeleteModelResponseParams
            {
            };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                ModelOperations.DeleteModel(dbConnection, parameters);
                return response;
            });
        }

        /// <summary>
        /// Handles download model request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleDownloadModelRequest(DownloadModelRequestParams parameters, RequestContext<DownloadModelResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleDownloadModelRequest");
            DownloadModelResponseParams response = new DownloadModelResponseParams
            {
            };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                response.FilePath = ModelOperations.DownloadModel(dbConnection, parameters);
                return response;
            });
        }

        /// <summary>
        /// Handles verify model table request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleVerifyModelTableRequest(VerifyModelTableRequestParams parameters, RequestContext<VerifyModelTableResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleVerifyModelTableRequest");
            VerifyModelTableResponseParams response = new VerifyModelTableResponseParams
            {
            };

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                response.Verified = ModelOperations.VerifyImportTable(dbConnection, parameters);
                return response;
            });
        }

        /// <summary>
        /// Handles configure model table request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task HandleConfigureModelTableRequest(ConfigureModelTableRequestParams parameters, RequestContext<ConfigureModelTableResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleConfigureModelTableRequest");
            ConfigureModelTableResponseParams response = new ConfigureModelTableResponseParams();

            await HandleRequest(parameters, response, requestContext, (dbConnection, parameters, response) =>
            {
                ModelOperations.ConfigureImportTable(dbConnection, parameters);
                return response;
            });
        }

        private async Task HandleRequest<T, TResponse>(
            T parameters, 
            TResponse response, 
            RequestContext<TResponse> requestContext,
            Func<IDbConnection, T, TResponse, TResponse> operation) where T : ModelRequestBase where TResponse : ModelResponseBase
        {
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                if (connInfo == null)
                {
                    await requestContext.SendError(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        response = operation(dbConnection, parameters, response);
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
