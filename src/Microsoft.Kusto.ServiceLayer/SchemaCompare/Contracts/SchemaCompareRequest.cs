//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    public enum SchemaCompareEndpointType
    {
        Database,
        Dacpac
    }

    public class SchemaCompareEndpointInfo
    {
        /// <summary>
        /// Gets or sets the type of the endpoint
        /// </summary>
        public SchemaCompareEndpointType EndpointType { get; set; }

        /// <summary>
        /// Gets or sets package filepath
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets name for the database
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Connection details
        /// </summary>
        public ConnectionDetails ConnectionDetails { get; set; }
    }

    /// <summary>
    /// Parameters for a schema compare request.
    /// </summary>
    public class SchemaCompareParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo SourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo TargetEndpointInfo { get; set; }

        /// <summary>
        /// Executation mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// gets or sets the deployment options for schema compare
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }
    }

    /// <summary>
    /// Parameters returned from a schema compare request.
    /// </summary>
    public class SchemaCompareResult : ResultStatus
    {
        public string OperationId { get; set; }

        public bool AreEqual { get; set; }

        public List<DiffEntry> Differences { get; set; }
    }

    public class DiffEntry
    {
        public SchemaUpdateAction UpdateAction { get; set; }
        public SchemaDifferenceType DifferenceType { get; set; }
        public string Name { get; set; }
        public string[] SourceValue { get; set; }
        public string[] TargetValue { get; set; }
        public DiffEntry Parent { get; set; }
        public List<DiffEntry> Children { get; set; }
        public string SourceScript { get; set; }
        public string TargetScript { get; set; }
        public string SourceObjectType { get; set; }
        public string TargetObjectType { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare request type
    /// </summary>
    class SchemaCompareRequest
    {
        public static readonly RequestType<SchemaCompareParams, SchemaCompareResult> Type =
            RequestType<SchemaCompareParams, SchemaCompareResult>.Create("schemaCompare/compare");
    }
}
