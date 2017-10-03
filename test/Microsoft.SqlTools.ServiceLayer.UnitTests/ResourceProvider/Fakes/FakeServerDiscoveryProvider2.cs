//------------------------------------------------------------------------------
// <copyright file="RdtManager.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{   
    [Exportable(ServerTypeValue, CategoryValue, typeof(IServerDiscoveryProvider), 
    "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeServerDiscoveryProvider2")]
    public class FakeServerDiscoveryProvider2 : ExportableBase, IServerDiscoveryProvider
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

        public const string ServerTypeValue = "FakeServerType";
        public const string CategoryValue = "FakeCategory";
    }
}

