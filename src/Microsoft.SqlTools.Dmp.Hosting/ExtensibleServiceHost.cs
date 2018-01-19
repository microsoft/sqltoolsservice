//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Dmp.Contracts;
using Microsoft.SqlTools.Dmp.Contracts.Hosting;
using Microsoft.SqlTools.Dmp.Hosting.Channels;
using Microsoft.SqlTools.Dmp.Hosting.Extensibility;
using Microsoft.SqlTools.Dmp.Hosting.Utility;

namespace Microsoft.SqlTools.Dmp.Hosting
{
    public class ExtensibleServiceHost : ServiceHost
    {
        private readonly RegisteredServiceProvider serviceProvider;
        
        #region Construction

        public ExtensibleServiceHost(
            RegisteredServiceProvider provider,
            ChannelBase protocolChannel,
            ProviderDetails details,
            LanguageServiceCapabilities capabilities)
            : base(protocolChannel, details, capabilities)
        {
            Validate.IsNotNull(nameof(provider), provider);
            
            provider.RegisterHostedServices();
            
            // Initialize all hosted services
            foreach (IHostedService service in provider.GetServices<IHostedService>())
            {
                service.InitializeService(this);
            }

            serviceProvider = provider;
        }

        /// <summary>
        /// Constructs a new service host intended to be used as a JSON RPC server. StdIn is used
        /// for receiving messages, StdOut is used for sending messages. Services will be
        /// discovered from the assemblies in the current directory listed in 
        /// <paramref name="assembliesToInclude"/>
        /// </summary>
        /// <param name="directory">Directory to include assemblies from</param>
        /// <param name="assembliesToInclude">
        /// List of assembly names in the current directory to search for service exports
        /// </param>
        /// <param name="details">Details about the protocol this service host will provide</param>
        /// <param name="capabilities">Language service capabilities</param>
        /// <returns>Service host as a JSON RPC server over StdI/O</returns>
        public static ExtensibleServiceHost CreateDefaultExtensibleServer(
            string directory,
            IList<string> assembliesToInclude,
            ProviderDetails details,
            LanguageServiceCapabilities capabilities)
        {
            Validate.IsNotNull(nameof(assembliesToInclude), assembliesToInclude);
            
            ExtensionServiceProvider serviceProvider = ExtensionServiceProvider.CreateFromAssembliesInDirectory(directory, assembliesToInclude);
            return new ExtensibleServiceHost(serviceProvider, new StdioServerChannel(), details, capabilities);
        }

        #endregion

        public IMultiServiceProvider ServiceProvider => serviceProvider;
    }
}