//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Drivers.SqlClient;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>Server-free SqlClient adapter logic: connection-string building and error mapping.</summary>
    public class SqlClientUnitTests
    {
        private static ConnectionOpenRequest Request(SecretMaterial auth, Dictionary<string, string>? options = null) => new()
        {
            Server = "tcp:host,1433",
            Database = "appdb",
            Auth = auth,
            ConnectTimeoutMs = 15000,
            ApplicationName = "sts2-test",
            Options = options ?? new Dictionary<string, string>(),
        };

        [Fact]
        public void SqlLoginBuildsUserAndPasswordWithoutLeakingIntoToken()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "sqlLogin", User = "sa", Secret = "p@ss" }));

            Assert.Contains("User ID=sa", connectionString);
            Assert.Contains("Password=p@ss", connectionString); // builder owns this; redaction happens upstream of Core
            Assert.Contains("Initial Catalog=appdb", connectionString);
            Assert.Contains("Application Name=sts2-test", connectionString);
            Assert.Null(token);
        }

        [Fact]
        public void AccessTokenGoesToTokenNotConnectionString()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "accessToken", User = "u", Secret = "jwt-material" }));

            Assert.Equal("jwt-material", token);
            Assert.DoesNotContain("jwt-material", connectionString);
            Assert.DoesNotContain("Password=", connectionString);
        }

        [Fact]
        public void IntegratedSetsIntegratedSecurity()
        {
            (string connectionString, string? token) = SqlClientConnectionString.Build(
                Request(new SecretMaterial { Kind = "integrated" }));
            Assert.Contains("Integrated Security=True", connectionString);
            Assert.Null(token);
        }

        [Theory]
        [InlineData("strict", "Strict")]
        [InlineData("true", "True")]    // Mandatory serializes as True for back-compat
        [InlineData("false", "False")]  // Optional serializes as False
        public void EncryptOptionMapsToBuilderEnum(string optionValue, string expected)
        {
            (string connectionString, _) = SqlClientConnectionString.Build(Request(
                new SecretMaterial { Kind = "integrated" },
                new Dictionary<string, string> { ["encrypt"] = optionValue }));
            Assert.Contains("Encrypt=" + expected, connectionString);
        }

        [Fact]
        public void UnsupportedAuthKindThrowsStableDriverException()
        {
            DbDriverException ex = Assert.Throws<DbDriverException>(() =>
                SqlClientConnectionString.Build(Request(new SecretMaterial { Kind = "kerberosMagic" })));
            Assert.Equal("Sts2.InvalidRequest", ex.Code);
        }
    }
}
