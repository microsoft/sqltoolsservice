//------------------------------------------------------------------------------
// <copyright file="RdtManager.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    public interface IDatabaseResourceManager
    {
    }

    //[Export(typeof(IDatabaseResourceManager))]
    [Exportable(FakeServerDiscoveryProvider.ServerTypeValue, FakeServerDiscoveryProvider.CategoryValue
    , typeof(IDatabaseResourceManager), "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeDatabaseResourceManager")]
    public class FakeDatabaseResourceManager : IDatabaseResourceManager
    {
    }
}
