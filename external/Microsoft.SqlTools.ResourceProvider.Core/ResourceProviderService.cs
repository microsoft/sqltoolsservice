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
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ResourceProvider.Core
{

    [Export(typeof(IHostedService))]
    public class ResourceProviderService : HostedService<ResourceProviderService>, IComposableService
    {
        private const string MssqlProviderId = "MSSQL";

        private FirewallRuleService firewallRuleService;
        /// <summary>
        /// The default constructor is required for MEF-based composable services
        /// </summary>
        public ResourceProviderService()
        {
        }
        
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(TraceEventType.Verbose, "ResourceProvider initialized");
            serviceHost.SetRequestHandler(CreateFirewallRuleRequest.Type, HandleCreateFirewallRuleRequest);
            serviceHost.SetRequestHandler(CanHandleFirewallRuleRequest.Type, ProcessHandleFirewallRuleRequest);

            firewallRuleService = new FirewallRuleService()
            {
                AuthenticationManager = ServiceProvider.GetService<IAzureAuthenticationManager>(),
                ResourceManager = ServiceProvider.GetService<IAzureResourceManager>()
            };
        }
        
        /// <summary>
        /// Handles a firewall rule creation request. It does this by matching the server name to an Azure Server resource,
        /// then issuing the command to create a new firewall rule for the specified IP address against that instance
        /// </summary>
        /// <param name="firewallRule"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public async Task HandleCreateFirewallRuleRequest(CreateFirewallRuleParams firewallRule, RequestContext<CreateFirewallRuleResponse> requestContext)
        {
            Func<Task<CreateFirewallRuleResponse>> requestHandler = () =>
            {
                return DoHandleCreateFirewallRuleRequest(firewallRule);
            };
            Func<ExpiredTokenException, CreateFirewallRuleResponse> tokenExpiredHandler = (ExpiredTokenException ex) =>
            {
                return new CreateFirewallRuleResponse()
                {
                    Result = false,
                    IsTokenExpiredFailure = true,
                    ErrorMessage = ex.Message
                };
            };
            await HandleRequest(requestHandler, tokenExpiredHandler, requestContext, "HandleCreateFirewallRuleRequest");
        }

        private async Task<CreateFirewallRuleResponse> DoHandleCreateFirewallRuleRequest(CreateFirewallRuleParams firewallRule)
        {
            var result = new CreateFirewallRuleResponse();
            // Note: currently not catching the exception. Expect the caller to this message to handle error cases by
            // showing the error string and responding with a clean failure message to the user
            try
            {
                AuthenticationService authService = ServiceProvider.GetService<AuthenticationService>();
                IUserAccount account = await authService.SetCurrentAccountAsync(firewallRule.Account, firewallRule.SecurityTokenMappings);
                FirewallRuleResponse response = await firewallRuleService.CreateFirewallRuleAsync(firewallRule.ServerName, firewallRule.StartIpAddress, firewallRule.EndIpAddress);
                result.Result = true;
            }
            catch(FirewallRuleException ex)
            {
                result.Result = false;
                result.ErrorMessage = ex.Message;
            }
            return result;
        }
        
        public async Task ProcessHandleFirewallRuleRequest(HandleFirewallRuleParams canHandleRuleParams, RequestContext<HandleFirewallRuleResponse> requestContext)
        {
            Func<Task<HandleFirewallRuleResponse>> requestHandler = () =>
            {
                HandleFirewallRuleResponse response = new HandleFirewallRuleResponse();
                if (!MssqlProviderId.Equals(canHandleRuleParams.ConnectionTypeId, StringComparison.OrdinalIgnoreCase))
                {
                    response.Result = false;
                    response.ErrorMessage = SR.FirewallRuleUnsupportedConnectionType;
                }
                else
                {
                    FirewallErrorParser parser = new FirewallErrorParser();
                    FirewallParserResponse parserResponse = parser.ParseErrorMessage(canHandleRuleParams.ErrorMessage, canHandleRuleParams.ErrorCode);
                    response.Result = parserResponse.FirewallRuleErrorDetected;
                    response.IpAddress = parserResponse.BlockedIpAddress != null ? parserResponse.BlockedIpAddress.ToString() : string.Empty;
                }
                return Task.FromResult(response);
            };
            await HandleRequest(requestHandler, null, requestContext, "HandleCreateFirewallRuleRequest");
        }
        
        private async Task HandleRequest<T>(Func<Task<T>> handler, Func<ExpiredTokenException, T> expiredTokenHandler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(TraceEventType.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch(ExpiredTokenException ex)
            {
                if (expiredTokenHandler != null)
                {
                    // This is a special exception indicating the token(s) used to request resources had expired.
                    // Any Azure resource should have handling for this such as an error path that clearly indicates that a refresh is needed
                    T result = expiredTokenHandler(ex);
                    await requestContext.SendResult(result);
                }
                else
                {
                    // No handling for expired tokens defined / expected
                    await requestContext.SendError(ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Send just the error message back for now as stack trace isn't useful
                await requestContext.SendError(ex.Message);
            }
        }   
    }
}
