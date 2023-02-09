//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Authentication
{
    /// <summary>
    /// Parameters to be passed to <see cref="Authenticator"/> to request an access token
    /// </summary>
    public class AuthenticationParams
    {
        /// <summary>
        /// Authentication method to be used by <see cref="Authenticator"/>.
        /// </summary>
        public AuthenticationMethod AuthenticationMethod { get; set; }

        /// <summary>
        /// Authority URL, e.g. https://login.microsoftonline.com/
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Audience for which access token should be acquired, e.g. common, organizations, consumers.
        /// It can also be a tenant Id when authenticating multi-tenant application accounts.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Array of scopes for which access token is requested.
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// <see cref="Guid"/> Connection Id, that will be passed to Azure AD when requesting access token.
        /// It can be used for tracking AAD request status if needed.
        /// </summary>
        public Guid ConnectionId { get; set; }

        /// <summary>
        /// User name to be provided as userhint when acquiring access token.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="authMethod">Authentication Method to be used.</param>
        /// <param name="authority">Authority URL</param>
        /// <param name="audience">Audience</param>
        /// <param name="scopes">Scopes for access token</param>
        /// <param name="username">User hint information</param>
        /// <param name="connectionId">Connection Id for tracing AAD request</param>
        public AuthenticationParams(AuthenticationMethod authMethod, string authority, string audience, 
            string[] scopes, string username, Guid connectionId) {
            this.AuthenticationMethod = authMethod;
            this.Authority = authority;
            this.Audience = audience;
            this.Scopes = scopes;
            this.Username = username;
            this.ConnectionId = connectionId;
        }
    }
}
