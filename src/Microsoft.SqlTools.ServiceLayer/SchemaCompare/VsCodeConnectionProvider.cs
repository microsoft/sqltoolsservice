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
    /// Bridges ConnectionService to the host-agnostic interface.
    /// </summary>
    internal class VsCodeConnectionProvider : ISchemaCompareConnectionProvider
    {
        private readonly ConnectionService _connectionService;

        public VsCodeConnectionProvider(ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo)
        {
            if (endpointInfo.OwnerUri == null)
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

        public string GetAccessToken(SchemaCompareEndpointInfo endpointInfo)
        {
            if (endpointInfo.OwnerUri == null)
            {
                return null;
            }

            ConnectionInfo connInfo;
            _connectionService.TryFindConnection(endpointInfo.OwnerUri, out connInfo);
            if (connInfo?.ConnectionDetails?.AzureAccountToken != null && connInfo.ConnectionDetails.AuthenticationType == AzureMFA)
            {
                return connInfo.ConnectionDetails.AzureAccountToken;
            }

            return null;
        }

        public SchemaCompareEndpointInfo ParseConnectionString(string connectionString)
        {
            var connectionDetails = _connectionService.ParseConnectionString(connectionString);
            return new SchemaCompareEndpointInfo
            {
                ServerName = connectionDetails.ServerName,
                DatabaseName = connectionDetails.DatabaseName,
                UserName = connectionDetails.UserName,
                ConnectionString = connectionString
            };
        }
    }
}
