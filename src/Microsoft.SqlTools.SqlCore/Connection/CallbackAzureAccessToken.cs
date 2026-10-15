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
    /// An <see cref="IRenewableToken"/> implementation that fetches Azure access tokens via an
    /// async callback.  Used when the client host provides tokens via
    /// <c>account/securityTokenRequest</c> (RequestMfaTokenFromClient mode) rather than MSAL.
    /// Tokens are not cached here: the client owns the token cache, so each request goes to it.
    /// </summary>
    public class CallbackAzureAccessToken : IRenewableToken
    {
        private readonly Func<Task<(string token, DateTimeOffset expiresOn)>> _tokenFetcher;

        // IRenewableToken props; not used in the callback path.
        public DateTimeOffset TokenExpiry { get; set; }
        public string Resource { get; set; }
        public string Tenant { get; set; }
        public string UserId { get; set; }

        /// <param name="tokenFetcher">Delegate that fetches a fresh token on demand.</param>
        public CallbackAzureAccessToken(Func<Task<(string token, DateTimeOffset expiresOn)>> tokenFetcher)
        {
            _tokenFetcher = tokenFetcher ?? throw new ArgumentNullException(nameof(tokenFetcher));
        }

        /// <summary>
        /// Returns an access token fetched via the callback.
        /// </summary>
        public string GetAccessToken() => _tokenFetcher().GetAwaiter().GetResult().token;
    }
}
