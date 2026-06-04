//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility
{
    public class ExternalLanguageService
    {
        private ExternalLanguageOperations serviceOperations = new ExternalLanguageOperations();
        private ConnectionService connectionService = null;
        private static readonly Lazy<ExternalLanguageService> instance = new Lazy<ExternalLanguageService>(() => new ExternalLanguageService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static ExternalLanguageService Instance
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

        public ExternalLanguageOperations ExternalLanguageOperations
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
            serviceHost.RegisterRequestHandler(ExternalLanguageStatusRequest.Type, this.HandleExternalLanguageStatusRequest);
            serviceHost.RegisterRequestHandler(ExternalLanguageListRequest.Type, this.HandleExternalLanguageListRequest);
            serviceHost.RegisterRequestHandler(ExternalLanguageDeleteRequest.Type, this.HandleExternalLanguageDeleteRequest);
            serviceHost.RegisterRequestHandler(ExternalLanguageUpdateRequest.Type, this.HandleExternalLanguageUpdateRequest);
        }

        /// <summary>
        /// Handles external language delete request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task<ExternalLanguageDeleteResponseParams> HandleExternalLanguageDeleteRequest(ExternalLanguageDeleteRequestParams parameters)
        {
            Logger.Verbose("HandleExternalLanguageDeleteRequest");
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            ExternalLanguageDeleteResponseParams response = new ExternalLanguageDeleteResponseParams
            {
            };

            if (connInfo == null)
            {
                throw RpcErrorException.Create(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
            }
            else
            {
                using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                {
                    ExternalLanguageOperations.DeleteLanguage(dbConnection, parameters.LanguageName);
                }

                return response;
            }
        }

        /// <summary>
        /// Handles external language delete request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task<ExternalLanguageUpdateResponseParams> HandleExternalLanguageUpdateRequest(ExternalLanguageUpdateRequestParams parameters)
        {
            Logger.Verbose("HandleExternalLanguageUpdateRequest");
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            ExternalLanguageUpdateResponseParams response = new ExternalLanguageUpdateResponseParams
            {
            };

            if (connInfo == null)
            {
                throw RpcErrorException.Create(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
            }
            else
            {
                using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                {
                    ExternalLanguageOperations.UpdateLanguage(dbConnection, parameters.Language);
                }

                return response;
            }
        }

        /// <summary>
        /// Handles external language status request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task<ExternalLanguageStatusResponseParams> HandleExternalLanguageStatusRequest(ExternalLanguageStatusRequestParams parameters)
        {
            Logger.Verbose("HandleExternalLanguageStatusRequest");
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            ExternalLanguageStatusResponseParams response = new ExternalLanguageStatusResponseParams
            {
                Status = false,
            };

            if (connInfo == null)
            {
                throw RpcErrorException.Create(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
            }
            else
            {
                using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                {
                    response.Status = ExternalLanguageOperations.GetLanguageStatus(dbConnection, parameters.LanguageName);
                }

                return response;
            }
        }

        /// <summary>
        /// Handles external language status request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task<ExternalLanguageListResponseParams> HandleExternalLanguageListRequest(ExternalLanguageListRequestParams parameters)
        {
            Logger.Verbose("HandleExternalLanguageListRequest");
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(
                parameters.OwnerUri,
                out connInfo);
            ExternalLanguageListResponseParams response = new ExternalLanguageListResponseParams
            {
            };

            if (connInfo == null)
            {
                throw RpcErrorException.Create(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
            }
            else
            {
                using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                {
                    response.Languages = ExternalLanguageOperations.GetLanguages(dbConnection);
                }

                return response;
            }
        }
    }
}
