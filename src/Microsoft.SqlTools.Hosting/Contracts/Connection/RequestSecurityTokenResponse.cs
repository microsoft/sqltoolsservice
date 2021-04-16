namespace Microsoft.SqlTools.Hosting.Contracts.Connection
{
    public class RequestSecurityTokenResponse
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
}