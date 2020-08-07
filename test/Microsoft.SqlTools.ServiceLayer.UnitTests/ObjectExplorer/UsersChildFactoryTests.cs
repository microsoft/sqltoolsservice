//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectExplorer
{
    public class UsersChildFactoryTests
    {
        [Test]
        public void GetStatusShouldReturnEmptyStringGivenNull()
        {
            string expected = string.Empty;
            string actual = UserCustomeNodeHelper.GetStatus(null);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void GetStatusShouldReturnEmptyStringGivenNotUser()
        {
            string expected = string.Empty;
            string actual = UserCustomeNodeHelper.GetStatus(new Database());
            Assert.AreEqual(expected, actual);
        }
    }
}
