//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.Kusto.ServiceLayer.Connection;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Connection
{
    public class ConnectionProviderOptionsHelperTests
    {
        [Test]
        public void BuildConnectionProviderOptions_Returns_30_Options()
        {
            var providerOptions = ConnectionProviderOptionsHelper.BuildConnectionProviderOptions();
            Assert.AreEqual(30, providerOptions.Options.Length);
        }
    }
}