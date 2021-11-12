//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare publish project changes request.
    /// </summary>
    public class SchemaComparePublishProjectChangesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Path of project folder
        /// </summary>
        public string TargetProjectPath { get; set; }

        /// <summary>
        /// folder structure of target folder
        /// </summary>
        public DacExtractTarget TargetFolderStructure { get; set; }

        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare publish project changes request type
    /// </summary>
    class SchemaComparePublishProjectChangesRequest
    {
        public static readonly RequestType<SchemaComparePublishProjectChangesParams, SchemaComparePublishProjectResult> Type =
            RequestType<SchemaComparePublishProjectChangesParams, SchemaComparePublishProjectResult>.Create("schemaCompare/publishProject");
    }
}