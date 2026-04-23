//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Wraps an async token-fetching delegate and caches the result until the token is close to
    /// expiry, avoiding redundant round-trips to the client via <c>account/securityTokenRequest</c>.
    ///
    /// Thread-safe: concurrent callers block on a <see cref="SemaphoreSlim"/> and re-use the token
    /// fetched by whichever caller wins the lock, preventing thundering-herd refreshes.
    /// </summary>
    internal sealed class CachingTokenFetcher
    {
        /// <summary>
        /// How far before the token's <c>ExpiresOn</c> timestamp a refresh is triggered.
        /// Matches the window used by <see cref="Microsoft.SqlTools.SqlCore.Connection.CallbackAzureAccessToken"/>.
        /// </summary>
        internal static readonly TimeSpan EarlyRefreshWindow = TimeSpan.FromMinutes(2);

        private readonly Func<Task<(string token, DateTimeOffset expiresOn)>> _inner;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private string? _cachedToken;
        private DateTimeOffset _cachedExpiresOn = DateTimeOffset.MinValue;

        internal CachingTokenFetcher(Func<Task<(string token, DateTimeOffset expiresOn)>> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Returns <c>true</c> when the cached token exists and will not expire within
        /// <see cref="EarlyRefreshWindow"/>.
        /// </summary>
        internal bool IsCacheValid()
            => _cachedToken != null && DateTimeOffset.UtcNow < _cachedExpiresOn - EarlyRefreshWindow;

        /// <summary>
        /// Returns a valid access token, either from the cache or by invoking the inner delegate.
        /// </summary>
        public async Task<(string token, DateTimeOffset expiresOn)> GetTokenAsync()
        {
            // Fast path — skip the semaphore when the cache is clearly warm.
            if (IsCacheValid())
            {
                return (_cachedToken!, _cachedExpiresOn);
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // Re-check after acquiring the semaphore: a concurrent caller may have already
                // refreshed the token while we were waiting.
                if (IsCacheValid())
                {
                    return (_cachedToken!, _cachedExpiresOn);
                }

                var (token, expiresOn) = await _inner().ConfigureAwait(false);
                _cachedToken = token;
                _cachedExpiresOn = expiresOn;
                return (token, expiresOn);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
