//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.SqlCore.SchemaCompare;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using SLContracts = Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SchemaCompare
{
    /// <summary>
    /// Test implementation of ISchemaCompareConnectionProvider that wraps ConnectionInfo
    /// for integration tests that directly instantiate SqlCore operations.
    /// </summary>
    internal sealed class TestConnectionProvider : ISchemaCompareConnectionProvider
    {
        private readonly ConnectionInfo _connectionInfo;

        public TestConnectionProvider(ConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
        }

        public string GetConnectionString(SchemaCompareEndpointInfo endpointInfo)
        {
            if (_connectionInfo == null)
            {
                return null;
            }

            // Mirror the original ServiceLayer.SchemaCompareUtils.GetConnectionString behavior:
            // override the database name from the endpoint info before building the connection string,
            // so that source and target Database endpoints each connect to their respective databases.
            if (!string.IsNullOrEmpty(endpointInfo?.DatabaseName))
            {
                _connectionInfo.ConnectionDetails.DatabaseName = endpointInfo.DatabaseName;
            }
            return ConnectionService.BuildConnectionString(_connectionInfo.ConnectionDetails);
        }

        public string GetAccessToken(SchemaCompareEndpointInfo endpointInfo)
        {
            return null;
        }

        public SchemaCompareEndpointInfo ParseConnectionString(string connectionString)
        {
            return new SchemaCompareEndpointInfo();
        }
    }

    /// <summary>
    /// Static helper methods for converting ServiceLayer contract types to SqlCore contract types
    /// in integration tests that directly instantiate SqlCore operations.
    /// </summary>
    internal static class SchemaCompareTestConverters
    {
        /// <summary>
        /// Converts a ServiceLayer SchemaCompareEndpointInfo to a SqlCore SchemaCompareEndpointInfo.
        /// </summary>
        public static SchemaCompareEndpointInfo ToCoreEndpoint(SLContracts.SchemaCompareEndpointInfo endpoint)
        {
            if (endpoint == null) return null;
            return new SchemaCompareEndpointInfo
            {
                EndpointType = endpoint.EndpointType,
                ProjectFilePath = endpoint.ProjectFilePath,
                TargetScripts = endpoint.TargetScripts,
                DataSchemaProvider = endpoint.DataSchemaProvider,
                PackageFilePath = endpoint.PackageFilePath,
                DatabaseName = endpoint.DatabaseName,
                OwnerUri = endpoint.OwnerUri,
                ExtractTarget = endpoint.ExtractTarget
            };
        }

        /// <summary>
        /// Converts a ServiceLayer SchemaCompareParams to a SqlCore SchemaCompareParams.
        /// </summary>
        public static SchemaCompareParams ToCoreParams(SLContracts.SchemaCompareParams slParams)
        {
            if (slParams == null) return null;
            return new SchemaCompareParams
            {
                OperationId = slParams.OperationId,
                SourceEndpointInfo = ToCoreEndpoint(slParams.SourceEndpointInfo),
                TargetEndpointInfo = ToCoreEndpoint(slParams.TargetEndpointInfo),
                DeploymentOptions = slParams.DeploymentOptions
            };
        }

        /// <summary>
        /// Converts a ServiceLayer SchemaCompareSaveScmpParams to a SqlCore SchemaCompareSaveScmpParams.
        /// </summary>
        public static SchemaCompareSaveScmpParams ToCoreSaveScmpParams(SLContracts.SchemaCompareSaveScmpParams slParams)
        {
            if (slParams == null) return null;
            return new SchemaCompareSaveScmpParams
            {
                OperationId = slParams.OperationId,
                SourceEndpointInfo = ToCoreEndpoint(slParams.SourceEndpointInfo),
                TargetEndpointInfo = ToCoreEndpoint(slParams.TargetEndpointInfo),
                DeploymentOptions = slParams.DeploymentOptions,
                ScmpFilePath = slParams.ScmpFilePath,
                ExcludedSourceObjects = slParams.ExcludedSourceObjects,
                ExcludedTargetObjects = slParams.ExcludedTargetObjects
            };
        }
    }
}
