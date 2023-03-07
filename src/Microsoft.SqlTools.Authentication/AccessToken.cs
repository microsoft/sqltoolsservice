//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Authentication
{
    /// <summary>
    /// Represents an access token data object.
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// OAuth 2.0 JWT encoded access token string
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Expiry date of token
        /// </summary>
        public DateTimeOffset ExpiresOn { get; set; }

        /// <summary>
        /// Default constructor for Access Token object
        /// </summary>
        /// <param name="token">Access token as string</param>
        /// <param name="expiresOn">Expiry date</param>
        public AccessToken(string token, DateTimeOffset expiresOn) {
            this.Token = token;
            this.ExpiresOn = expiresOn;
        }
    }
}
