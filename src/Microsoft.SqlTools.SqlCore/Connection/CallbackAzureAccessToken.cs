//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlTools.SqlCore.Connection
{
    /// <summary>
    /// An <see cref="IRenewableToken"/> implementation that fetches and caches Azure access tokens
    /// via an async callback.  Used when the client host provides tokens via
    /// <c>account/securityTokenRequest</c> (RequestMfaTokenFromClient mode) rather than MSAL.
    /// </summary>
    public class CallbackAzureAccessToken : IRenewableToken
    {
        private readonly Func<Task<(string token, DateTimeOffset expiresOn)>> _tokenFetcher;
        private string _cachedToken;
        private DateTimeOffset _expiresOn;
        private readonly object _lock = new object();

        // IRenewableToken metadata — not used in the callback path.
        public DateTimeOffset TokenExpiry { get; set; }
        public string Resource { get; set; }
        public string Tenant { get; set; }
        public string UserId { get; set; }

        public CallbackAzureAccessToken(Func<Task<(string token, DateTimeOffset expiresOn)>> tokenFetcher)
        {
            _tokenFetcher = tokenFetcher ?? throw new ArgumentNullException(nameof(tokenFetcher));
            _expiresOn = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Returns a valid access token, fetching a fresh one via the callback when the cached
        /// token is absent or within 2 minutes of expiry.
        /// </summary>
        public string GetAccessToken()
        {
            lock (_lock)
            {
                if (_cachedToken == null || DateTimeOffset.UtcNow >= _expiresOn.AddMinutes(-2))
                {
                    var (token, expiresOn) = _tokenFetcher().GetAwaiter().GetResult();
                    _cachedToken = token;
                    _expiresOn = expiresOn;
                }
                return _cachedToken;
            }
        }
    }
}
