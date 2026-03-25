//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.SqlTools.ResourceProvider.DefaultImpl
{
    /// <summary>
    /// A <see cref="TokenCredential"/> that wraps a static, pre-acquired access token.
    /// Used when the auth flow has already obtained a token string and no refresh is needed.
    /// </summary>
    public sealed class StaticTokenCredential : TokenCredential
    {
        private readonly AccessToken _token;

        public StaticTokenCredential(string token, string tokenType = "Bearer")
        {
            _token = new AccessToken(token, DateTimeOffset.MaxValue);
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => _token;

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => ValueTask.FromResult(_token);
    }
}
