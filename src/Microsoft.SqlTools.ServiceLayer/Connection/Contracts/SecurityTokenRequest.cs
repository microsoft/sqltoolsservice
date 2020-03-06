using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    class RequestSecurityTokenParams 
    {
        /// <summary>
        /// Gets or sets authority.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets resource.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets scope.
        /// </summary>
        public string Scope { get; set; }
    }

    class RequestSecurityTokenResponse
    {
        /// <summary>
        /// Gets or sets userName.
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// ConnectionComplete notification mapping entry 
    /// </summary>
    class SecurityTokenRequest
    {
        public static readonly
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse> Type =
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse>.Create("account/securityTokenRequest");
    }
}
