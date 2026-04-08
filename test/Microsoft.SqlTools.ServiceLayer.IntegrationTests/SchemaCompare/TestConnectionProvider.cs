//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using static Microsoft.SqlTools.Utility.SqlConstants;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Test implementation of ISchemaCompareConnectionProvider for integration tests.
    /// Wraps source and target ConnectionInfo directly.
    /// </summary>
    internal sealed class TestConnectionProvider : ISchemaCompareConnectionProvider
    {
        private readonly ConnectionInfo _sourceConnInfo;
        private readonly ConnectionInfo _targetConnInfo;

        public TestConnectionProvider(ConnectionInfo sourceConnInfo, ConnectionInfo targetConnInfo)
        {
            _sourceConnInfo = sourceConnInfo;
            _targetConnInfo = targetConnInfo;
        }

        public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo)
        {
            // Determine which connection info to use based on endpoint
            ConnectionInfo connInfo = FindConnectionInfo(endpointInfo);
            if (connInfo == null)
            {
                return null;
            }

            connInfo.ConnectionDetails.DatabaseName = endpointInfo.DatabaseName;
            return ConnectionService.BuildConnectionString(connInfo.ConnectionDetails);
        }

        public string GetAccessToken(SchemaCompareEndpointInfo endpointInfo)
        {
            ConnectionInfo connInfo = FindConnectionInfo(endpointInfo);
            if (connInfo?.ConnectionDetails?.AzureAccountToken != null && connInfo.ConnectionDetails.AuthenticationType == AzureMFA)
            {
                return connInfo.ConnectionDetails.AzureAccountToken;
            }

            return null;
        }

        public SchemaCompareEndpointInfo ParseConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return new SchemaCompareEndpointInfo
            {
                ServerName = builder.DataSource,
                DatabaseName = builder.InitialCatalog,
                UserName = builder.UserID,
                ConnectionString = connectionString
            };
        }

        private ConnectionInfo FindConnectionInfo(SchemaCompareEndpointInfo endpointInfo)
        {
            // In tests, source/target determination is based on which ConnectionInfo was provided
            // Try source first, fall back to target
            if (_sourceConnInfo != null && endpointInfo.OwnerUri != null &&
                _sourceConnInfo.OwnerUri == endpointInfo.OwnerUri)
            {
                return _sourceConnInfo;
            }

            if (_targetConnInfo != null && endpointInfo.OwnerUri != null &&
                _targetConnInfo.OwnerUri == endpointInfo.OwnerUri)
            {
                return _targetConnInfo;
            }

            // If no OwnerUri match, return source or target based on availability
            return _sourceConnInfo ?? _targetConnInfo;
        }
    }
}
