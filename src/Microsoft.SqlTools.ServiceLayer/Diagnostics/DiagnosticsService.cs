//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Diagnostics.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Diagnostics
{
    public class DiagnosticsService
    {

        public void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "DiagnosticsService initialized");
            serviceHost.SetRequestHandler(DiagnosticsRequest.Type, ProcessDiagnosticsRequest);
        }

        public async Task ProcessDiagnosticsRequest(DiagnosticsParams diagnosticsParams, RequestContext<ProviderErrorCode> requestContext)
        {
            Func<Task<ProviderErrorCode>> requestHandler = () =>
            {
                // Check if provider is MSSQL
                bool isMssql = DiagnosticsConstants.MssqlProviderId.Equals(diagnosticsParams.ConnectionTypeId, StringComparison.OrdinalIgnoreCase);
                
                // Check if error is for MSSQL Password Reset
                bool isMssqlPWReset = DiagnosticsConstants.MssqlPasswordResetCode.Equals(diagnosticsParams.ErrorCode);
                
                ProviderErrorCode response = ProviderErrorCode.noErrorOrUnsupported;
                if (isMssql)
                {
                    if(isMssqlPWReset) {
                        response = ProviderErrorCode.passwordReset;
                    }
                }
                return Task.FromResult(response);
            };
            await HandleRequest(requestHandler, requestContext, "DiagnosticsRequest");
        }

         public async Task ProcessPasswordChangeRequest(ChangePasswordParams changePasswordParams, RequestContext<PasswordChangeResponse> requestContext)
        {
            Func<Task<PasswordChangeResponse>> requestHandler = () =>
            {
                // Check if provider is MSSQL
                bool isMssql = DiagnosticsConstants.MssqlProviderId.Equals(changePasswordParams.ConnectionTypeId, StringComparison.OrdinalIgnoreCase);

                // TODO need to get connection service to change password and validate.
                // ConnectionService.ChangePassword(changePasswordParams);
                PasswordChangeResponse response = new PasswordChangeResponse();
                response.Result = true;
                return Task.FromResult(response);
            };
            await HandleRequest(requestHandler, requestContext, "HandleChangePasswordRequest");
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
