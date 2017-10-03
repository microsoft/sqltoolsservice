//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
