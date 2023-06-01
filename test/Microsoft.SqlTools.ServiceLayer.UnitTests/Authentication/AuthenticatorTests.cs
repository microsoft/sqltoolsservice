//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Authentication;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Authentication
{
    [TestFixture]
    public class AuthenticatorTests
    {
        [Test]
        [TestCase("John Doe - johndoe@constoso.com", "johndoe@constoso.com")]
        [TestCase("John Doe - john-doe@constoso.com", "john-doe@constoso.com")]
        [TestCase("John Doe (Manager - Sales) - johndoe@constoso.com", "johndoe@constoso.com")]
        [TestCase("John - Doe (Manager - Sales) - john-doe@constoso.com", "john-doe@constoso.com")]
        [TestCase("John Doe - johndoe@constoso-sales.com", "johndoe@constoso-sales.com")]
        [TestCase("johndoe@constoso.com", "johndoe@constoso.com")]
        [TestCase("johndoe@constoso-sales.com", "johndoe@constoso-sales.com")]
        public async Task GetTokenAsyncExtractsEmailSuccessfully(string username, string expectedEmail)
        {
            Authenticator authenticator = new Authenticator(new SqlTools.Authentication.Utility.AuthenticatorConfiguration(
                Guid.NewGuid().ToString(), "AppName", ".", "dummyCacheFile"), () => ("key", "iv"));
            try
            {
                await authenticator.GetTokenAsync(new AuthenticationParams(AuthenticationMethod.ActiveDirectoryInteractive,
                    "https://login.microsoftonline.com/",
                    "common",
                    "https://database.windows.net/",
                    new string[] {
                        "https://database.windows.net/.default"
                    },
                    username,
                    Guid.Empty),
                CancellationToken.None);
                Assert.Fail("Expected exception did not occur.");
            }
            catch (Exception e)
            {
                Assert.False(e.Message.StartsWith("Invalid email address format", StringComparison.OrdinalIgnoreCase), $"Email address format should be correct, message received: {e.Message}");
                Assert.True(e.Message.Contains($"User account '{expectedEmail}' not found in MSAL cache, please add linked account or refresh account credentials."), $"Expected error did not occur, message received: {e.Message}");
            }
        }
    }
}
