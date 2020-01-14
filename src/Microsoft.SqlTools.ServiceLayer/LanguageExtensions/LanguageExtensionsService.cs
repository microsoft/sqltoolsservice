//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensions.Contracts;
using Microsoft.SqlTools.Utility;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensions
{
    public class LanguageExtensionsService
    {
        private LanguageExtensionOperations servcieOperations = new LanguageExtensionOperations();
        private ConnectionService connectionService = null;
        private static readonly Lazy<LanguageExtensionsService> instance = new Lazy<LanguageExtensionsService>(() => new LanguageExtensionsService());

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static LanguageExtensionsService Instance
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
            serviceHost.SetRequestHandler(ExternalLanguageStatusRequest.Type, this.HandleExternalLanguageStatusRequest);
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
                    await requestContext.SendError(new Exception(SR.ProfilerConnectionNotFound));
                }
                else
                {
                    using (IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo))
                    {
                        response.Status = servcieOperations.GetLanguageStatus(dbConnection, parameters.LanguageName);
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
