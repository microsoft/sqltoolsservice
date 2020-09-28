using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// SecurityToken Request mapping entry 
    /// </summary>
    public class SecurityTokenRequest
    {
        public static readonly
            RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse> Type =
                RequestType<RequestSecurityTokenParams, RequestSecurityTokenResponse>.Create(
                    "account/securityTokenRequest");
    }
}