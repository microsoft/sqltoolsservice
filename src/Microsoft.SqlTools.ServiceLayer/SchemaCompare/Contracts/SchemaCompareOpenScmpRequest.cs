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
    public class SchemaCompareObjectId
    {
        /// <summary>
        /// Name to create object identifier
        /// </summary>
        public string Name;

        /// <summary>
        /// sql object type
        /// </summary>
        public string SqlObjectType;
    }

    /// <summary>
    /// Parameters for a schema compare open scmp file request.
    /// </summary>
    public class SchemaCompareOpenScmpParams
    {
        /// <summary>
        /// filepath of scmp
        /// </summary>
        public string filePath { get; set; }
    }

    /// <summary>
    /// Parameters returned from a schema compare open scmp request.
    /// </summary>
    public class SchemaCompareOpenScmpResult : ResultStatus
    {
        /// <summary>
        /// Gets or sets the current source endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo SourceEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the current target endpoint info
        /// </summary>
        public SchemaCompareEndpointInfo TargetEndpointInfo { get; set; }

        /// <summary>
        /// Gets or sets the original target name. This is the initial target name, not necessarily the same as TargetEndpointInfo if they were swapped
        /// The original target name is used to determine whether to use ExcludedSourceElements or ExcludedTargetElements if source and target were swapped
        /// </summary>
        public string OriginalTargetName { get; set; }

        /// <summary>
        /// Gets or sets the original target connection string. This is the initial target connection string, not necessarily the same as TargetEndpointInfo if they were swapped
        /// The target connection string is necessary if the source and target are a dacpac and db with the same name
            /// </summary>
        public string OriginalTargetConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the deployment options
        /// </summary>
        public DeploymentOptions DeploymentOptions { get; set; }

        /// <summary>
        /// Gets or sets the excluded source elements. This is based on the initial source, not necessarily the same as SourceEndpointInfo if they were swapped
        /// </summary>
        public List<SchemaCompareObjectId> ExcludedSourceElements { get; set; }

        /// <summary>
        /// Gets or sets the excluded target elements. This is based on the initial target, not necessarily the same as TargetEndpointInfo if they were swapped
        /// </summary>
        public List<SchemaCompareObjectId> ExcludedTargetElements { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare open scmp request type
    /// </summary>
    class SchemaCompareOpenScmpRequest
    {
        public static readonly RequestType<SchemaCompareOpenScmpParams, SchemaCompareOpenScmpResult> Type =
            RequestType<SchemaCompareOpenScmpParams, SchemaCompareOpenScmpResult>.Create("schemaCompare/openScmp");
    }
}
