//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    public interface IDatabaseResourceManager
    {
    }
    
    [Exportable(FakeServerDiscoveryProvider.ServerTypeValue, FakeServerDiscoveryProvider.CategoryValue
    , typeof(IDatabaseResourceManager), "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeDatabaseResourceManager")]
    public class FakeDatabaseResourceManager : IDatabaseResourceManager
    {
    }
}
