//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Authentication
{
    public interface IAuthenticator
    {
        /// <summary>
        /// Acquires access token synchronously.
        /// </summary>
        /// <param name="params">Authentication parameters to be used for access token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Access Token with expiry date</returns>
        public AccessToken? GetToken(AuthenticationParams @params, CancellationToken cancellationToken);

        /// <summary>
        /// Acquires access token asynchronously.
        /// </summary>
        /// <param name="params">Authentication parameters to be used for access token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Access Token with expiry date</returns>
        /// <exception cref="Exception"></exception>
        public Task<AccessToken?> GetTokenAsync(AuthenticationParams @params, CancellationToken cancellationToken);
    }
}
