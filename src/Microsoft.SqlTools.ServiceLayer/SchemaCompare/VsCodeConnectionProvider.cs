//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using static Microsoft.SqlTools.Utility.SqlConstants;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// VSCode/ADS implementation of ISchemaCompareConnectionProvider.
    /// Delegates to ConnectionService for connection lookups and string building.
    /// </summary>
    internal class VsCodeConnectionProvider : ISchemaCompareConnectionProvider
    {
        private readonly ConnectionService _connectionService;

        public VsCodeConnectionProvider(ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// Builds a connection string for the given endpoint using the ConnectionService.
        /// </summary>
        public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo)
        {
            if (endpointInfo == null || string.IsNullOrEmpty(endpointInfo.OwnerUri))
            {
                return null;
            }

            ConnectionInfo connInfo;
            _connectionService.TryFindConnection(endpointInfo.OwnerUri, out connInfo);
            if (connInfo == null)
            {
                return null;
            }

            connInfo.ConnectionDetails.DatabaseName = endpointInfo.DatabaseName;
            return ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
        }

        /// <summary>
        /// Returns the Azure access token if the connection uses Azure MFA authentication.
        /// Returns null otherwise so that the caller uses standard authentication.
        /// </summary>
        public string GetAccessToken(SchemaCompareEndpointInfo endpointInfo)
        {
            if (endpointInfo == null || string.IsNullOrEmpty(endpointInfo.OwnerUri))
            {
                return null;
            }

            ConnectionInfo connInfo;
            _connectionService.TryFindConnection(endpointInfo.OwnerUri, out connInfo);
            if (connInfo?.ConnectionDetails?.AuthenticationType == AzureMFA
                && connInfo.ConnectionDetails.AzureAccountToken != null)
            {
                return connInfo.ConnectionDetails.AzureAccountToken;
            }
            return null;
        }

        /// <summary>
        /// Parses a connection string and returns a SchemaCompareEndpointInfo with
        /// ServerName, DatabaseName, UserName, and ConnectionString populated.
        /// </summary>
        public SchemaCompareEndpointInfo ParseConnectionString(string connectionString)
        {
            var details = _connectionService.ParseConnectionString(connectionString);
            return new SchemaCompareEndpointInfo
            {
                DatabaseName = details.DatabaseName,
                ServerName = details.ServerName,
                UserName = details.UserName,
                ConnectionString = connectionString
            };
        }
    }
}
