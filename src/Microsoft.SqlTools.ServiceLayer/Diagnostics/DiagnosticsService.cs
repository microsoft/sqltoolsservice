//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Diagnostics.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Diagnostics
{
    public class DiagnosticsService
    {
         #region Singleton Instance Implementation

        private static readonly Lazy<DiagnosticsService> LazyInstance = new Lazy<DiagnosticsService>(() => new DiagnosticsService());

        public static DiagnosticsService Instance => LazyInstance.Value;

        #endregion

         /// <summary>
        /// Default, parameterless constructor.
        /// </summary>
        internal DiagnosticsService()
        {
        }

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "DiagnosticsService initialized");
            serviceHost.SetRequestHandler(DiagnosticsRequest.Type, ProcessDiagnosticsRequest);
        }

        public async Task ProcessDiagnosticsRequest(DiagnosticsParams diagnosticsParams, RequestContext<DiagnosticsResponse> requestContext)
        {
            Func<Task<DiagnosticsResponse>> requestHandler = () =>
            {
                // Check if error is for MSSQL Password Reset
                bool isMssqlExpiredPassword = DiagnosticsConstants.MssqlPasswordResetCode.Equals(diagnosticsParams.ErrorCode);
                bool isMssqlWrongPassword = DiagnosticsConstants.MssqlFailedLogin.Equals(diagnosticsParams.ErrorCode);

                DiagnosticsResponse response = new DiagnosticsResponse();
                response.ErrorAction = "";
                if(isMssqlExpiredPassword) {
                    response.ErrorAction = DiagnosticsConstants.MssqlExpiredPassword;
                }
                else if (isMssqlWrongPassword) {
                    response.ErrorAction = DiagnosticsConstants.MssqlWrongPassword;
                }
                return Task.FromResult(response);
            };
            await HandleRequest(requestHandler, requestContext, "DiagnosticsRequest");
        }

        private async Task HandleRequest<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(TraceEventType.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {  
                await requestContext.SendError(ex.Message); 
            }
        }
    }
}
