//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.SqlCore.Connection;
using Microsoft.SqlTools.Utility;

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
        private readonly Func<Task<(string token, DateTimeOffset expiresOn)>> FetchNewToken;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        private string _cachedToken;
        private DateTimeOffset _cachedExpiresOn = DateTimeOffset.MinValue;

        internal CachingTokenFetcher(Func<Task<(string token, DateTimeOffset expiresOn)>> fetchNewToken)
        {
            FetchNewToken = fetchNewToken ?? throw new ArgumentNullException(nameof(fetchNewToken));
        }

        /// <summary>
        /// Returns <c>true</c> when the cached token exists and will not expire within
        /// <see cref="CallbackAzureAccessToken.EarlyRefreshWindow"/>.
        /// </summary>
        internal bool IsCacheValid()
            => _cachedToken != null && DateTimeOffset.UtcNow < _cachedExpiresOn - CallbackAzureAccessToken.EarlyRefreshWindow;

        /// <summary>
        /// Returns a valid access token, either from the cache or by invoking the inner delegate.
        /// </summary>
        public async Task<(string token, DateTimeOffset expiresOn)> GetTokenAsync()
        {
            // Fast path — skip the semaphore when the cache is clearly warm.
            if (IsCacheValid())
            {
                Logger.Verbose("Azure token cache hit");
                return (_cachedToken, _cachedExpiresOn);
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // Re-check after acquiring the semaphore: a concurrent caller may have already
                // refreshed the token while we were waiting.
                if (IsCacheValid())
                {
                    Logger.Verbose("Azure token cache hit (post-lock)");
                    return (_cachedToken, _cachedExpiresOn);
                }

                Logger.Verbose("Requesting Azure access token from client");
                var (token, expiresOn) = await FetchNewToken().ConfigureAwait(false);
                _cachedToken = token;
                _cachedExpiresOn = expiresOn;
                Logger.Information($"Azure access token acquired successfully, expires {expiresOn}");

                return (token, expiresOn);
            }
            catch (Exception ex)
            {
                Logger.Error($"Azure access token fetch failed: {ex.Message}");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
