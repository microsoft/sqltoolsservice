//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using NUnit.Framework;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Admin
{
    [TestFixture]
    /// <summary>
    /// Tests for AdminService Class
    /// </summary>
    public class AdminServiceTests
    {
        [Test]
        public void TestBuildingSecureStringFromPassword()
        {
            string password = "test_password";
            var secureString = CDataContainer.BuildSecureStringFromPassword(password);
            Assert.AreEqual(password.Length, secureString.Length);
        }

        [Test]
        public void TestBuildingSecureStringFromNullPassword()
        {
            string password = null;
            var secureString = CDataContainer.BuildSecureStringFromPassword(password);
            Assert.AreEqual(0, secureString.Length);
        }
    }
}
