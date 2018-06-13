//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Security;
using Xunit;

using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Admin
{
    /// <summary>
    /// Tests for AdminService Class
    /// </summary>
    public class AdminServiceTests
    {
        [Fact]
        public void TestBuildingSecureStringFromPassword()
        {
            string password = "test_password";
            var secureString = CDataContainer.BuildSecureStringFromPassword(password);
            Assert.Equal(password.Length, secureString.Length);
        }

        [Fact]
        public void TestBuildingSecureStringFromNullPassword()
        {
            string password = null;
            var secureString = CDataContainer.BuildSecureStringFromPassword(password);
            Assert.Equal(0, secureString.Length);
        }
    }
}
