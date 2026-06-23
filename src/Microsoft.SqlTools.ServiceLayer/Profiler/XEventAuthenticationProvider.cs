//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// Temporary shim that lets XELite's <c>XELiveEventStreamer</c> authenticate against
    /// Azure SQL when STS is running in <c>--request-mfa-token-from-client</c> mode.
    /// </summary>
    internal class XEventAuthenticationProvider : SqlAuthenticationProvider
    {
        private static readonly ConcurrentDictionary<(string accountId, string tenantId), Func<string, Task<(string token, DateTimeOffset expiresOn)>>> s_fetchers =
            new ConcurrentDictionary<(string accountId, string tenantId), Func<string, Task<(string token, DateTimeOffset expiresOn)>>>();

        private static readonly System.Threading.Lock s_registrationLock = new System.Threading.Lock();
        private static bool s_registered;

        /// <summary>
        /// Registers a token fetcher for the given <paramref name="accountId"/> / <paramref name="tenantId"/>
        /// pair, and ensures this provider is registered with SqlClient for
        /// <see cref="SqlAuthenticationMethod.ActiveDirectoryInteractive"/>. Safe to call
        /// repeatedly; later registrations replace earlier ones for the same key.
        /// </summary>
        public static void Register(
            string accountId,
            string tenantId,
            Func<string, Task<(string token, DateTimeOffset expiresOn)>> fetcher)
        {
            if (string.IsNullOrEmpty(accountId) || fetcher == null)
            {
                return;
            }

            s_fetchers[(accountId, tenantId ?? string.Empty)] = fetcher;
            EnsureProviderRegistered();
        }

        /// <summary>
        /// Exposed for tests. Removes all registered fetchers and resets the provider registration
        /// so that each test starts from a clean state.
        /// </summary>
        internal static void ClearForTests()
        {
            s_fetchers.Clear();
            lock (s_registrationLock)
            {
                s_registered = false;
            }
        }

        private static void EnsureProviderRegistered()
        {
            if (s_registered)
            {
                return;
            }

            lock (s_registrationLock)
            {
                if (s_registered)
                {
                    return;
                }

                SqlAuthenticationProvider.SetProvider(
                    SqlAuthenticationMethod.ActiveDirectoryInteractive,
                    new XEventAuthenticationProvider());
                s_registered = true;
            }
        }

        public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var accountId = parameters.UserId ?? string.Empty;
            var tenantId = ExtractTenantFromAuthority(parameters.Authority);

            if (!s_fetchers.TryGetValue((accountId, tenantId), out var fetcher))
            {
                // Fallback: try any entry registered for this account regardless of tenant. This
                // covers the rare case where the authority URL tenant doesn't exactly match what
                // was registered (e.g. tenant GUID vs. friendly-name mismatch).
                var fallback = s_fetchers.FirstOrDefault(kvp => kvp.Key.accountId == accountId);
                if (fallback.Value == null)
                {
                    // This should never happen in normal operation. XEventAuthenticationProvider
                    // is only activated when ProfilerService calls Register() with the same
                    // accountId embedded in the XELite connection string's User ID field. If we
                    // reach here, the connection string was built with an accountId for which no
                    // fetcher was registered — indicating a programming error or an unexpected
                    // code path.
                    string message = $"XEventAuthenticationProvider: no token fetcher registered for " +
                        $"account '{accountId}' (tenant '{tenantId}'). This is a bug — the provider " +
                        $"should only be invoked for accounts registered via {nameof(Register)}.";
                    Logger.Error(message);
                    throw new InvalidOperationException(message);
                }
                fetcher = fallback.Value;
            }

            if (fetcher == null)
            {
                throw new Exception($"Unable to acquire token fetcher for account '{accountId}' (tenant '{tenantId}')");
            }

            var (token, expiresOn) = await fetcher(parameters.Resource).ConfigureAwait(false);
            return new SqlAuthenticationToken(token, expiresOn);
        }

        public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
            => authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryInteractive;

        /// <summary>
        /// Parses the tenant id from a SqlClient-provided authority URL.
        /// Examples: <c>https://login.microsoftonline.com/{tenantId}</c>,
        /// <c>https://login.windows.net/{tenantId}/</c>.
        /// </summary>
        internal static string ExtractTenantFromAuthority(string authority)
        {
            if (string.IsNullOrEmpty(authority))
            {
                return string.Empty;
            }

            var trimmed = authority.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
        }
    }
}
