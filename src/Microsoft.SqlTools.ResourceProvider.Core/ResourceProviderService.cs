//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Composition;
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
        private FirewallRuleService firewallRuleService;
        /// <summary>
        /// The default constructor is required for MEF-based composable services
        /// </summary>
        public ResourceProviderService()
        {
        }
        
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(LogLevel.Verbose, "TSqlFormatter initialized");
            serviceHost.SetRequestHandler(CreateFirewallRuleRequest.Type, HandleCreateFirewallRuleRequest);

            firewallRuleService = new FirewallRuleService()
            {
                AuthenticationManager = ServiceProvider.GetService<IAzureAuthenticationManager>(),
                ResourceManager = ServiceProvider.GetService<IAzureResourceManager>()
            };
        }
        
        public async Task HandleCreateFirewallRuleRequest(FirewallRule firewallRule, RequestContext<bool> requestContext)
        {
            Func<Task<bool>> requestHandler = () =>
            {
                return DoHandleCreateFirewallRuleRequest(firewallRule);
            };
            await HandleRequest(requestHandler, requestContext, "HandleCreateFirewallRuleRequest");
        }

        private async Task<bool> DoHandleCreateFirewallRuleRequest(FirewallRule firewallRule)
        {
            // Note: currently not catching the exception. Expect the caller to this message to handle error cases by
            // showing the error string and responding with a clean failure message to the user
            FirewallRuleResponse response = await firewallRuleService.CreateFirewallRuleAsync(firewallRule.ServerName, firewallRule.StartIpAddressValue, firewallRule.EndIpAddressValue);
            return response.Created;
        }

        private async Task HandleRequest<T>(Func<Task<T>> handler, RequestContext<T> requestContext, string requestType)
        {
            Logger.Write(LogLevel.Verbose, requestType);

            try
            {
                T result = await handler();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }   
    }
}
