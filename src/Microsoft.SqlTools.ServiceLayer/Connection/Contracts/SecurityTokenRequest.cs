//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    class RequestSecurityTokenParams
    {
        /// <summary>
        /// Gets or sets the provider that indicates the type of linked account to query.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the authority URL from where token is requested.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the scope array of the authentication request.
        /// </summary>
        public string [] Scopes { get; set; }

        /// <summary>
        /// Gets or sets the Entra account ID
        /// Populated only when RequestMfaTokenFromClient is enabled; null for MSAL callers.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets or sets the Entra tenant ID
        /// Populated only when RequestMfaTokenFromClient is enabled; null for MSAL callers.
        /// </summary>
        public string TenantId { get; set; }
    }

    class RequestSecurityTokenResponse
    {
        /// <summary>
        /// Gets or sets the key that uniquely identifies a particular linked account.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the token expiration as Unix epoch.
        /// </summary>
        public long ExpiresOn { get; set; }
    }

    class SecurityTokenRequest
    {
        public static readonly
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse> Type =
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse>.Create("account/securityTokenRequest");
    }
}
