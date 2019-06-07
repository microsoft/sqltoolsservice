//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare publish load scmp file request.
    /// </summary>
    public class SchemaCompareLoadScmpParams
    {
        /// <summary>
        /// filepath of scmp
        /// </summary>
        public string filePath { get; set; }
    }

    /// <summary>
    /// Parameters returned from a schema compare load scmp request.
    /// </summary>
    public class SchemaCompareLoadScmpResult : ResultStatus
    {
        /// <summary>
        /// Gets or sets the source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo SourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo TargetEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the deployment options
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }

        public List<string> ExcludedSourceElements { get; set; }

        public List<string> ExcludedTargetElements { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare load scmp request type
    /// </summary>
    class SchemaCompareLoadScmpRequest
    {
        public static readonly RequestType<SchemaCompareLoadScmpParams, SchemaCompareLoadScmpResult> Type =
            RequestType<SchemaCompareLoadScmpParams, SchemaCompareLoadScmpResult>.Create("schemaCompare/loadScmp");
    }
}
