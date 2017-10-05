//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    [Exportable("SqlServer", "azure", typeof(IServerDiscoveryProvider),
        "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeSecureServerDiscoveryProvider")]
    public class FakeSecureServerDiscoveryProvider :  ExportableBase, IServerDiscoveryProvider, ISecureService
    {
        public FakeSecureServerDiscoveryProvider(IExportableMetadata metadata)
        {
            Metadata = metadata;
        }

        public async Task<ServiceResponse<ServerInstanceInfo>> GetServerInstancesAsync()
        {
            return await Task.Run(() => new ServiceResponse<ServerInstanceInfo>());
        }      
        
        public IDatabaseResourceManager DatabaseResourceManager
        {
            get;
            set;
        }
       
        public IAccountManager AccountManager
        {
            get
            {
                return GetService<IAccountManager>();
            }
            set
            {
                
            }
        }

    }
}
