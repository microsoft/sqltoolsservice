//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Data;
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
            serviceHost.RegisterRequestHandler(ImportModelRequest.Type, this.HandleModelImportRequest);
            serviceHost.RegisterRequestHandler(ConfigureModelTableRequest.Type, this.HandleConfigureModelTableRequest);
            serviceHost.RegisterRequestHandler(DeleteModelRequest.Type, this.HandleDeleteModelRequest);
            serviceHost.RegisterRequestHandler(DownloadModelRequest.Type, this.HandleDownloadModelRequest);
            serviceHost.RegisterRequestHandler(GetModelsRequest.Type, this.HandleGetModelsRequest);
            serviceHost.RegisterRequestHandler(UpdateModelRequest.Type, this.HandleUpdateModelRequest);
            serviceHost.RegisterRequestHandler(VerifyModelTableRequest.Type, this.HandleVerifyModelTableRequest);
        }

        /// <summary>
        /// Handles import model request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        public async Task<ImportModelResponseParams> HandleModelImportRequest(ImportModelRequestParams parameters)
        {
            Logger.Verbose("HandleModelImportRequest");
            ImportModelResponseParams response = new ImportModelResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<GetModelsResponseParams> HandleGetModelsRequest(GetModelsRequestParams parameters)
        {
            Logger.Verbose("HandleGetModelsRequest");
            GetModelsResponseParams response = new GetModelsResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<UpdateModelResponseParams> HandleUpdateModelRequest(UpdateModelRequestParams parameters)
        {
            Logger.Verbose("HandleUpdateModelRequest");
            UpdateModelResponseParams response = new UpdateModelResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<DeleteModelResponseParams> HandleDeleteModelRequest(DeleteModelRequestParams parameters)
        {
            Logger.Verbose("HandleDeleteModelRequest");
            DeleteModelResponseParams response = new DeleteModelResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<DownloadModelResponseParams> HandleDownloadModelRequest(DownloadModelRequestParams parameters)
        {
            Logger.Verbose("HandleDownloadModelRequest");
            DownloadModelResponseParams response = new DownloadModelResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<VerifyModelTableResponseParams> HandleVerifyModelTableRequest(VerifyModelTableRequestParams parameters)
        {
            Logger.Verbose("HandleVerifyModelTableRequest");
            VerifyModelTableResponseParams response = new VerifyModelTableResponseParams
            {
            };

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
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
        public async Task<ConfigureModelTableResponseParams> HandleConfigureModelTableRequest(ConfigureModelTableRequestParams parameters)
        {
            Logger.Verbose("HandleConfigureModelTableRequest");
            ConfigureModelTableResponseParams response = new ConfigureModelTableResponseParams();

            return await HandleRequest(parameters, response, (dbConnection, parameters, response) =>
            {
                ModelOperations.ConfigureImportTable(dbConnection, parameters);
                return response;
            });
        }

        private async Task<TResponse> HandleRequest<T, TResponse>(
            T parameters,
            TResponse response,
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
                    throw RpcErrorException.Create(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        response = operation(dbConnection, parameters, response);
                    }
                    return response;
                }
            }
            catch (Exception e)
            {
                // Exception related to run task will be captured here
                Logger.Error(e);
                throw RpcErrorException.Create(e);
            }
        }
    }
}
