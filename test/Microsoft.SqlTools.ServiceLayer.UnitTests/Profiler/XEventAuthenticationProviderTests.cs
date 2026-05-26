//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Profiler
{
    /// <summary>
    /// Unit tests for <see cref="XEventAuthenticationProvider"/>.
    ///
    /// These cover the routing logic introduced to keep the query profiler working in
    /// "request MFA token from client" (VS Code account-based) mode, where XELite's internal
    /// <see cref="SqlConnection"/> would otherwise have no way to authenticate to Azure SQL.
    /// </summary>
    public class XEventAuthenticationProviderTests
    {
        [SetUp]
        public void Setup() => XEventAuthenticationProvider.ClearForTests();

        [TearDown]
        public void TearDown() => XEventAuthenticationProvider.ClearForTests();

        [Test]
        public void IsSupported_only_supports_ActiveDirectoryInteractive()
        {
            var provider = new XEventAuthenticationProvider();
            Assert.Multiple(() =>
            {
                Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryInteractive), Is.True);
                Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryDefault), Is.False);
                Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryPassword), Is.False);
                Assert.That(provider.IsSupported(SqlAuthenticationMethod.SqlPassword), Is.False);
                Assert.That(provider.IsSupported(SqlAuthenticationMethod.NotSpecified), Is.False);
            });
        }

        [TestCase("https://login.microsoftonline.com/tenant-123", "tenant-123")]
        [TestCase("https://login.microsoftonline.com/consumers", "consumers")]
        [TestCase("https://login.windows.net/contoso.onmicrosoft.com", "contoso.onmicrosoft.com")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void ExtractTenantFromAuthority_returns_segment_after_last_slash(string authority, string expected)
        {
            Assert.That(XEventAuthenticationProvider.ExtractTenantFromAuthority(authority), Is.EqualTo(expected));
        }

        [Test]
        public async Task Register_then_AcquireTokenAsync_returns_token_for_matching_account_and_tenant()
        {
            var expectedToken = "tok-" + Guid.NewGuid();
            var expectedExpiry = DateTimeOffset.UtcNow.AddHours(1);

            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-1",
                fetcher: () => Task.FromResult((expectedToken, expectedExpiry)));

            var provider = new XEventAuthenticationProvider();
            var parameters = CreateParameters(userId: "acct-1", authority: "https://login.microsoftonline.com/tenant-1");

            var result = await provider.AcquireTokenAsync(parameters);

            Assert.Multiple(() =>
            {
                Assert.That(result.AccessToken, Is.EqualTo(expectedToken));
                Assert.That(result.ExpiresOn, Is.EqualTo(expectedExpiry));
            });
        }

        [Test]
        public async Task AcquireTokenAsync_falls_back_to_any_fetcher_for_matching_account_when_tenant_differs()
        {
            // Registration carries a tenant, but the SqlClient-provided Authority specifies a different
            // one. The provider should fall back to any registration matching the account id so that
            // tenant-only mismatches don't break profiling.
            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-A",
                fetcher: () => Task.FromResult(("tok-A", DateTimeOffset.UtcNow.AddHours(1))));

            var provider = new XEventAuthenticationProvider();
            var parameters = CreateParameters(userId: "acct-1", authority: "https://login.microsoftonline.com/tenant-B");

            var result = await provider.AcquireTokenAsync(parameters);
            Assert.That(result.AccessToken, Is.EqualTo("tok-A"));
        }

        [Test]
        public void AcquireTokenAsync_throws_when_no_fetcher_registered_for_account()
        {
            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-1",
                fetcher: () => Task.FromResult(("tok-1", DateTimeOffset.UtcNow.AddHours(1))));

            var provider = new XEventAuthenticationProvider();
            var parameters = CreateParameters(userId: "unknown-account", authority: "https://login.microsoftonline.com/tenant-1");

            Assert.ThrowsAsync<InvalidOperationException>(() => provider.AcquireTokenAsync(parameters));
        }

        [Test]
        public void Register_ignores_empty_account_id_and_null_fetcher()
        {
            // None of these should add an entry that AcquireTokenAsync could later resolve.
            XEventAuthenticationProvider.Register(
                accountId: "",
                tenantId: "tenant-1",
                fetcher: () => Task.FromResult(("tok", DateTimeOffset.UtcNow)));

            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-1",
                fetcher: null);

            var provider = new XEventAuthenticationProvider();
            var parameters = CreateParameters(userId: "acct-1", authority: "https://login.microsoftonline.com/tenant-1");

            Assert.ThrowsAsync<InvalidOperationException>(() => provider.AcquireTokenAsync(parameters));
        }

        [Test]
        public async Task Register_replaces_existing_fetcher_for_same_account_and_tenant()
        {
            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-1",
                fetcher: () => Task.FromResult(("old-token", DateTimeOffset.UtcNow.AddHours(1))));

            XEventAuthenticationProvider.Register(
                accountId: "acct-1",
                tenantId: "tenant-1",
                fetcher: () => Task.FromResult(("new-token", DateTimeOffset.UtcNow.AddHours(2))));

            var provider = new XEventAuthenticationProvider();
            var parameters = CreateParameters(userId: "acct-1", authority: "https://login.microsoftonline.com/tenant-1");

            var result = await provider.AcquireTokenAsync(parameters);
            Assert.That(result.AccessToken, Is.EqualTo("new-token"));
        }

        private static SqlAuthenticationParameters CreateParameters(string userId, string authority)
        {
            // The SqlAuthenticationParameters constructor is internal to Microsoft.Data.SqlClient,
            // so we construct one via reflection (matching SqlClient's own invocation pattern).
            var ctor = typeof(SqlAuthenticationParameters).GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic)[0];

            return (SqlAuthenticationParameters)ctor.Invoke(new object[]
            {
                SqlAuthenticationMethod.ActiveDirectoryInteractive,
                "test-server",
                "test-db",
                "https://database.windows.net/",
                authority ?? string.Empty,
                userId ?? string.Empty,
                string.Empty,
                Guid.NewGuid(),
                30,
            });
        }
    }
}
