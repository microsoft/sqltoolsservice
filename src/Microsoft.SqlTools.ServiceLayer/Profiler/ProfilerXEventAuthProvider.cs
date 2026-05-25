//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.Profiler
{
    /// <summary>
    /// A <see cref="SqlAuthenticationProvider"/> used by the Profiler to surface
    /// pre-acquired Entra (AAD) access tokens to XELite's <c>XELiveEventStreamer</c>.
    /// </summary>
    /// <remarks>
    /// XELite's <c>XELiveEventStreamer</c> only accepts a connection string; it
    /// creates its own internal <see cref="SqlConnection"/> and there is no API
    /// to set <see cref="SqlConnection.AccessToken"/> or
    /// <c>AccessTokenCallback</c> on it. For Microsoft Entra MFA, the access
    /// token is acquired client-side (in VS Code) and pushed to STS, which then
    /// applies it programmatically to the SqlConnection it manages. That
    /// programmatic token is invisible to XELite's internal connection.
    /// <para/>
    /// To make MFA work with the live event streamer, the Profiler caches the
    /// access token (or a callback that can mint/refresh it) here, keyed by the
    /// account UserID, and hands XELite a connection string that uses
    /// <see cref="SqlAuthenticationMethod.ActiveDirectoryInteractive"/> with a
    /// matching <c>UserID</c>. SqlClient routes the authentication request
    /// through this provider, which returns the cached token without prompting
    /// the user.
    /// </remarks>
    internal sealed class ProfilerXEventAuthProvider : SqlAuthenticationProvider
    {
        private static readonly Lazy<ProfilerXEventAuthProvider> _instance =
            new Lazy<ProfilerXEventAuthProvider>(() => new ProfilerXEventAuthProvider());

        private static int _registered = 0;

        private readonly ConcurrentDictionary<string, Func<Task<SqlAuthenticationToken>>> _fetchers =
            new ConcurrentDictionary<string, Func<Task<SqlAuthenticationToken>>>(StringComparer.OrdinalIgnoreCase);

        private ProfilerXEventAuthProvider()
        {
        }

        public static ProfilerXEventAuthProvider Instance => _instance.Value;

        /// <summary>
        /// Registers this provider for <see cref="SqlAuthenticationMethod.ActiveDirectoryInteractive"/>
        /// on first call. Subsequent calls are no-ops.
        /// </summary>
        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 0)
            {
                SqlAuthenticationProvider.SetProvider(
                    SqlAuthenticationMethod.ActiveDirectoryInteractive,
                    Instance);
            }
        }

        /// <summary>
        /// Caches a static access token for the given user. Used when STS only
        /// has a one-shot token (the legacy <c>AzureAccountToken</c> path).
        /// </summary>
        public void CacheStaticToken(string userId, string token, DateTimeOffset expiresOn)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return;
            }
            _fetchers[userId] = () => Task.FromResult(new SqlAuthenticationToken(token, expiresOn));
        }

        /// <summary>
        /// Caches a token-fetching callback for the given user. Used when STS
        /// can refresh tokens via the VS Code account bridge.
        /// </summary>
        public void CacheTokenFetcher(
            string userId,
            Func<Task<(string token, DateTimeOffset expiresOn)>> tokenFetcher)
        {
            if (string.IsNullOrEmpty(userId) || tokenFetcher == null)
            {
                return;
            }
            _fetchers[userId] = async () =>
            {
                var (token, expiresOn) = await tokenFetcher().ConfigureAwait(false);
                return new SqlAuthenticationToken(token, expiresOn);
            };
        }

        /// <summary>
        /// Removes any cached token/fetcher for the given user. Safe to call
        /// even if no entry exists.
        /// </summary>
        public void Remove(string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                _fetchers.TryRemove(userId, out _);
            }
        }

        public override async Task<SqlAuthenticationToken> AcquireTokenAsync(
            SqlAuthenticationParameters parameters)
        {
            if (parameters != null
                && !string.IsNullOrEmpty(parameters.UserId)
                && _fetchers.TryGetValue(parameters.UserId, out var fetcher))
            {
                return await fetcher().ConfigureAwait(false);
            }

            throw new InvalidOperationException(
                $"ProfilerXEventAuthProvider has no cached token for user '{parameters?.UserId}'. "
                + "This provider is only intended for the Profiler's XELite session.");
        }

        public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
            => authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryInteractive;
    }
}
