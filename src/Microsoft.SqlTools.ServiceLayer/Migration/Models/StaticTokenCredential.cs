//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Models
{
    /// <summary>
    /// Temp token creadentials to interact with ArmClient class. 
    /// The token passed to this class should be a newly request token, because this class doesn't renew the token.
    /// Once MSAL is rolled out on ADS, we will implement a way to use the same ADS token cache configured by ADS. 
    /// </summary>
    internal class StaticTokenCredential : TokenCredential
    {
        private readonly AccessToken _token;

        /// <summary>
        /// Build credentials using a token that will not change.
        /// </summary>
        /// <param name="accessToken">Newly created token that should last for the duration of the whole operation.</param>
        public StaticTokenCredential(string accessToken) => _token = new AccessToken(
                   accessToken: accessToken,
                   expiresOn: DateTimeOffset.Now.AddHours(1));  //Default to an hour, the current duration of a newly create token.

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => _token;

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new ValueTask<AccessToken>(_token);
    }
}
