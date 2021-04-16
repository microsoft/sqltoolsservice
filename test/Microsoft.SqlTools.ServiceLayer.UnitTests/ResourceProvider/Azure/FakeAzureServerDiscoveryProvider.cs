//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure
{
    /// <summary>
    /// A fake server discovery class
    /// </summary>
    [Exportable(ServerTypes.SqlServer, Categories.Azure
        , typeof(IServerDiscoveryProvider),
        "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure.FakeAzureServerDiscoveryProvider", 1)]
    public class FakeAzureServerDiscoveryProvider : ExportableBase, IServerDiscoveryProvider, ISecureService
    {
        public FakeAzureServerDiscoveryProvider()
        {
            Metadata = new ExportableMetadata(ServerTypes.SqlServer, Categories.Azure, 
                "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Azure.FakeAzureServerDiscoveryProvider", 1);
        }
        public Task<ServiceResponse<ServerInstanceInfo>> GetServerInstancesAsync()
        {
            throw new NotImplementedException();
        }

        public IAccountManager AccountManager
        {
            get; 
            set;
        }

        /// <summary>
        /// This should always return null otherwise there's going to be a infinite loop
        /// </summary>
        public IServerDiscoveryProvider ServerDiscoveryProvider
        {
            get
            {
                return GetService<IServerDiscoveryProvider>();
            }
        }
    }
}
