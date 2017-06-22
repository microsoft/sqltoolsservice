//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class UsersChildFactoryTests
    {
        [Fact]
        public void GetStatusShouldReturnEmptyStringGivenNull()
        {
            string expected = string.Empty;
            string actual = UserCustomeNodeHelper.GetStatus(null);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetStatusShouldReturnEmptyStringGivenNotUser()
        {
            string expected = string.Empty;
            string actual = UserCustomeNodeHelper.GetStatus(new Database());
            Assert.Equal(expected, actual);
        }
    }
}
