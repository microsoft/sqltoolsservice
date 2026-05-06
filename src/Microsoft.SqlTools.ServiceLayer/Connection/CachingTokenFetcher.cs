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
    /// Wraps a token-fetching delegate and caches the result until the token is close to
    /// expiry, avoiding redundant round-trips to the client via <c>account/securityTokenRequest</c>.
    /// </summary>
    internal sealed class CachingTokenFetcher
    {
        private readonly Func<Task<(string token, DateTimeOffset expiresOn)>> FetchNewToken;

        /// <summary>
        /// Semaphore to ensure that only one fetch/refresh operation is in progress at a time for this token.
        /// </summary>
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
        /// Returns a valid access token, either from the cache or by invoking the fetch delegate.
        /// </summary>
        public async Task<(string token, DateTimeOffset expiresOn)> GetTokenAsync()
        {
            // 0. Return cached token if it's still valid
            if (IsCacheValid())
            {
                Logger.Verbose("Azure token cache hit");
                return (_cachedToken, _cachedExpiresOn);
            }

            // 1. Wait for exclusive access to refresh the token, in case multiple concurrent callers arrive when the cache is stale.
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // 2. After acquiring the semaphore, check the cache again before fetching a new token
                // in case a concurrent caller already refreshed it while we were waiting.
                if (IsCacheValid())
                {
                    Logger.Verbose("Azure token cache hit (post-lock)");
                    return (_cachedToken, _cachedExpiresOn);
                }

                // 3. Cached token is still invalid; fetch a new token from the client  
                Logger.Verbose("Requesting Azure access token from client");
                var (token, expiresOn) = await FetchNewToken().ConfigureAwait(false);
                Logger.Information($"Azure access token acquired successfully, expires {expiresOn}");

                _cachedToken = token;
                _cachedExpiresOn = expiresOn;

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
