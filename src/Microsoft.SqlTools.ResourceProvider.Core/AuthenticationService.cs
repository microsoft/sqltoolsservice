//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Contracts;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core.Firewall;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ResourceProvider.Core
{

    [Export(typeof(IHostedService))]
    public class AuthenticationService : HostedService<AuthenticationService>, IComposableService
    {
        /// <summary>
        /// The default constructor is required for MEF-based composable services
        /// </summary>
        public AuthenticationService()
        {
        }
        
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            Logger.Write(LogLevel.Verbose, "AuthenticationService initialized");
        }
        
        public async Task<IUserAccount> SetCurrentAccountAsync(Account account, Dictionary<string, string> securityTokenMappings)
        {
            var authManager = ServiceProvider.GetService<IAzureAuthenticationManager>();
            // Ideally in the future, would have a factory to create the user account and tenant info without knowing
            // which implementation is which. For expediency, will directly define these in this instance.
            return await authManager.SetCurrentAccountAsync(new AccountTokenWrapper(account, securityTokenMappings));
        }
    }

}
