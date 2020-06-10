//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.ObjectExplorer
{
    // Base class providing common test functionality for OE tests
    public abstract class ObjectExplorerTestBase : ServiceTestBase
    {
      
        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return CreateProvider()
                .RegisterSingleService(new ConnectionService())
                .RegisterSingleService(new ObjectExplorerService());
        }

        protected ObjectExplorerService CreateOEService(ConnectionService connService)
        {
            CreateProvider()
                .RegisterSingleService(connService)
                .RegisterSingleService(new ObjectExplorerService());
                // .RegisterSingleService<ChildFactory>(new ServerChildFactory());

            // Create the service using the service provider, which will initialize dependencies
            return ServiceProvider.GetService<ObjectExplorerService>();
        }

    }
}
