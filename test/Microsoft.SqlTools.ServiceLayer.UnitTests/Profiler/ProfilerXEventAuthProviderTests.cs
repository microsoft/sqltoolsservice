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
    /// Unit tests for <see cref="ProfilerXEventAuthProvider"/>.
    /// </summary>
    public class ProfilerXEventAuthProviderTests
    {
        [Test]
        public void IsSupported_only_for_ActiveDirectoryInteractive()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryInteractive), Is.True);
            Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryPassword), Is.False);
            Assert.That(provider.IsSupported(SqlAuthenticationMethod.ActiveDirectoryIntegrated), Is.False);
            Assert.That(provider.IsSupported(SqlAuthenticationMethod.SqlPassword), Is.False);
        }

        [Test]
        public async Task CacheStaticToken_returns_cached_value()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            string user = $"user-{Guid.NewGuid()}@example.com";
            var expires = DateTimeOffset.UtcNow.AddMinutes(30);
            try
            {
                provider.CacheStaticToken(user, "tok-123", expires);

                var parameters = MakeParameters(user);
                var token = await provider.AcquireTokenAsync(parameters);

                Assert.That(token, Is.Not.Null);
                Assert.That(token.AccessToken, Is.EqualTo("tok-123"));
                Assert.That(token.ExpiresOn, Is.EqualTo(expires));
            }
            finally
            {
                provider.Remove(user);
            }
        }

        [Test]
        public async Task CacheTokenFetcher_invokes_callback_each_time()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            string user = $"user-{Guid.NewGuid()}@example.com";
            int calls = 0;
            try
            {
                provider.CacheTokenFetcher(user, () =>
                {
                    calls++;
                    return Task.FromResult(($"tok-{calls}", DateTimeOffset.UtcNow.AddMinutes(5)));
                });

                var first = await provider.AcquireTokenAsync(MakeParameters(user));
                var second = await provider.AcquireTokenAsync(MakeParameters(user));

                Assert.That(first.AccessToken, Is.EqualTo("tok-1"));
                Assert.That(second.AccessToken, Is.EqualTo("tok-2"));
                Assert.That(calls, Is.EqualTo(2));
            }
            finally
            {
                provider.Remove(user);
            }
        }

        [Test]
        public void AcquireTokenAsync_throws_when_no_cache_entry()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            string user = $"missing-{Guid.NewGuid()}@example.com";
            Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.AcquireTokenAsync(MakeParameters(user)));
        }

        [Test]
        public void CacheStaticToken_ignores_null_or_empty_inputs()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            // Should not throw and should not register entries
            provider.CacheStaticToken(null, "tok", DateTimeOffset.UtcNow);
            provider.CacheStaticToken("", "tok", DateTimeOffset.UtcNow);
            provider.CacheStaticToken("user@x.com", null, DateTimeOffset.UtcNow);
            provider.CacheStaticToken("user@x.com", "", DateTimeOffset.UtcNow);

            Assert.ThrowsAsync<InvalidOperationException>(
                () => provider.AcquireTokenAsync(MakeParameters("user@x.com")));
        }

        [Test]
        public async Task User_lookup_is_case_insensitive()
        {
            var provider = ProfilerXEventAuthProvider.Instance;
            string user = $"User-{Guid.NewGuid()}@Example.com";
            try
            {
                provider.CacheStaticToken(user, "tok-ci", DateTimeOffset.UtcNow.AddMinutes(5));
                var token = await provider.AcquireTokenAsync(MakeParameters(user.ToLowerInvariant()));
                Assert.That(token.AccessToken, Is.EqualTo("tok-ci"));
            }
            finally
            {
                provider.Remove(user);
            }
        }

        private static SqlAuthenticationParameters MakeParameters(string userId)
        {
            return new TestAuthParameters(userId);
        }

        /// <summary>
        /// <see cref="SqlAuthenticationParameters"/> has a protected constructor, so
        /// derive a tiny shim purely for unit testing.
        /// </summary>
        private sealed class TestAuthParameters : SqlAuthenticationParameters
        {
            public TestAuthParameters(string userId)
                : base(
                    authenticationMethod: SqlAuthenticationMethod.ActiveDirectoryInteractive,
                    serverName: "example.database.windows.net",
                    databaseName: "db",
                    resource: "https://database.windows.net/",
                    authority: "https://login.microsoftonline.com/common",
                    userId: userId,
                    password: null,
                    connectionId: Guid.NewGuid(),
                    connectionTimeout: 30)
            {
            }
        }
    }
}
