//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    class RefreshTokenParams 
    {
        /// <summary>
        /// ID of the tenant
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the provider that indicates the type of linked account to query.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target resource of the requested token.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the account ID
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the account ID
        /// </summary>
        public string Uri { get; set; }

    }

    /// <summary>
    /// Refresh token request mapping entry 
    /// </summary>
    class RefreshTokenNotification
    {
        public static readonly
            EventType<RefreshTokenParams> Type =
            EventType<RefreshTokenParams>.Create("account/refreshToken");
    }

    class TokenRefreshedParams
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string Token { get; set; }

        /// <summmary>
        /// Gets or sets the token expiration, a Unix epoch 
        /// </summary>
        public int ExpiresOn { get; set; }

        /// <summmary>
        /// Connection URI
        /// </summary>
        public string Uri { get; set; }
    }

    class TokenRefreshedNotification
    {
        public static readonly
        EventType<TokenRefreshedParams> Type =
            EventType<TokenRefreshedParams>.Create("account/tokenRefreshed");
    }
}