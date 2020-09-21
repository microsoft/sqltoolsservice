namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    public class RequestSecurityTokenParams
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

        /// <summary>
        /// Gets or sets the server name of the authentication request.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the database name of the authentication request.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection id of the authentication request.
        /// </summary>
        public string ConnectionId { get; set; }
    }
}