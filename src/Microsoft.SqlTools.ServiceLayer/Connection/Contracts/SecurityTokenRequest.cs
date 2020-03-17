using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    class RequestSecurityTokenParams 
    {
        /// <summary>
        /// Gets or sets the address of the authority to issue token.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the provider that indicates the type of linked account to query.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the target resource that is the recipient of the requested token.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the scope of the authentication request.
        /// </summary>
        public string Scope { get; set; }
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
    }

    /// <summary>
    /// SecurityToken Request mapping entry 
    /// </summary>
    class SecurityTokenRequest
    {
        public static readonly
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse> Type =
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse>.Create("account/securityTokenRequest");
    }
}
