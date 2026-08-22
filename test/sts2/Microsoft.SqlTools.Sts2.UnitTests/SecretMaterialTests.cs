//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Sts2.Abstractions;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Security
{
    public class SecretMaterialTests
    {
        private const string Secret = "test-secret-never-log";

        [Fact]
        public void SecretMaterialToStringRedactsSecret()
        {
            var auth = new SecretMaterial
            {
                Kind = "sqlLogin",
                User = "test-user",
                Secret = Secret,
            };

            string text = auth.ToString();

            Assert.DoesNotContain(Secret, text);
            Assert.Contains("Secret = [REDACTED]", text);
        }

        [Fact]
        public void ConnectionOpenRequestToStringRedactsNestedSecret()
        {
            var request = new ConnectionOpenRequest
            {
                Server = "test-server",
                Auth = new SecretMaterial
                {
                    Kind = "accessToken",
                    User = "test-user",
                    Secret = Secret,
                },
            };

            string text = request.ToString();

            Assert.DoesNotContain(Secret, text);
            Assert.Contains("Secret = [REDACTED]", text);
        }
    }
}
