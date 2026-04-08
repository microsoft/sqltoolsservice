//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


namespace Microsoft.SqlTools.SqlCore.SchemaCompare
{
    /// <summary>
    /// Abstraction for connection-related operations needed by Schema Compare.
    /// VSCode implements this using ConnectionService; SSMS implements this using
    /// its own connection infrastructure.
    /// </summary>
    public interface ISchemaCompareConnectionProvider
    {
        /// <summary>
        /// Build a connection string for a database endpoint.
        /// </summary>
        /// <param name="endpointInfo">The endpoint information.</param>
        /// <returns>The connection string, or null if not applicable.</returns>
        string GetConnectionString(Contracts.SchemaCompareEndpointInfo endpointInfo);

        /// <summary>
        /// Get an access token for Azure MFA authentication, if applicable.
        /// </summary>
        /// <param name="endpointInfo">The endpoint information.</param>
        /// <returns>Access token string, or null if not using token auth.</returns>
        string GetAccessToken(Contracts.SchemaCompareEndpointInfo endpointInfo);

        /// <summary>
        /// Parse a connection string (from an SCMP file) into endpoint info
        /// with ServerName, DatabaseName, UserName, and ConnectionString populated.
        /// </summary>
        /// <param name="connectionString">The raw connection string.</param>
        /// <returns>Populated endpoint info.</returns>
        Contracts.SchemaCompareEndpointInfo ParseConnectionString(string connectionString);
    }
}
