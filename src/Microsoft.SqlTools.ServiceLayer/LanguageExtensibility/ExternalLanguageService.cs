//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data;
using System.Diagnostics;
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
            serviceHost.SetRequestHandler(ExternalLanguageStatusRequest.Type, this.HandleExternalLanguageStatusRequest);
            serviceHost.SetRequestHandler(ExternalLanguageListRequest.Type, this.HandleExternalLanguageListRequest);
            serviceHost.SetRequestHandler(ExternalLanguageDeleteRequest.Type, this.HandleExternalLanguageDeleteRequest);
            serviceHost.SetRequestHandler(ExternalLanguageUpdateRequest.Type, this.HandleExternalLanguageUpdateRequest);
        }

        /// <summary>
        /// Handles external language delete request
        /// </summary>
        /// <param name="parameters">Request parameters</param>
        /// <param name="requestContext">Request Context</param>
        /// <returns></returns>
        public async Task HandleExternalLanguageDeleteRequest(ExternalLanguageDeleteRequestParams parameters, RequestContext<ExternalLanguageDeleteResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalLanguageDeleteRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ExternalLanguageDeleteResponseParams response = new ExternalLanguageDeleteResponseParams
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
                        ExternalLanguageOperations.DeleteLanguage(dbConnection, parameters.LanguageName);
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
        public async Task HandleExternalLanguageUpdateRequest(ExternalLanguageUpdateRequestParams parameters, RequestContext<ExternalLanguageUpdateResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalLanguageUpdateRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ExternalLanguageUpdateResponseParams response = new ExternalLanguageUpdateResponseParams
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
                        ExternalLanguageOperations.UpdateLanguage(dbConnection, parameters.Language);
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
        public async Task HandleExternalLanguageStatusRequest(ExternalLanguageStatusRequestParams parameters, RequestContext<ExternalLanguageStatusResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalLanguageStatusRequest");
            try
            {
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
                    await requestContext.SendError(new Exception(SR.ConnectionServiceDbErrorDefaultNotConnected(parameters.OwnerUri)));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        response.Status = ExternalLanguageOperations.GetLanguageStatus(dbConnection, parameters.LanguageName);
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
        public async Task HandleExternalLanguageListRequest(ExternalLanguageListRequestParams parameters, RequestContext<ExternalLanguageListResponseParams> requestContext)
        {
            Logger.Write(TraceEventType.Verbose, "HandleExternalLanguageListRequest");
            try
            {
                ConnectionInfo connInfo;
                ConnectionServiceInstance.TryFindConnection(
                    parameters.OwnerUri,
                    out connInfo);
                ExternalLanguageListResponseParams response = new ExternalLanguageListResponseParams
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
                        response.Languages = ExternalLanguageOperations.GetLanguages(dbConnection);
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
