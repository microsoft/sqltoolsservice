//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using System;

namespace Microsoft.SqlTools.SqlCore.Utility
{
    public class AccessTokenProvider : IUniversalAuthProvider
    {
        private readonly Func<string> _accessTokenCallback;

        /// <summary>
        /// Creates an auth provider that always returns the given static access token.
        /// </summary>
        public AccessTokenProvider(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new ArgumentNullException("accessToken");
            }

            _accessTokenCallback = () => accessToken;
        }

        /// <summary>
        /// Creates an auth provider that invokes the supplied callback whenever a token is needed.
        /// Use this overload when the underlying token source can refresh tokens on demand
        /// (e.g. <c>ConnectionInfo.AzureTokenFetcher</c>).
        /// </summary>
        public AccessTokenProvider(Func<string> accessTokenCallback)
        {
            _accessTokenCallback = accessTokenCallback ?? throw new ArgumentNullException(nameof(accessTokenCallback));
        }

        public bool IsTokenExpired() { return false; }

        public string GetValidAccessToken() { return _accessTokenCallback(); }
    }
}
