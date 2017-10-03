//------------------------------------------------------------------------------
// <copyright file="RdtManager.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
