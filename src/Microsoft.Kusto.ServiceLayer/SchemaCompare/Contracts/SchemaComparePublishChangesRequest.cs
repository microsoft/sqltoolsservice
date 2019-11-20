//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.TaskServices;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare publish changes request.
    /// </summary>
    public class SchemaComparePublishChangesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Name of target server
        /// </summary>
        public string TargetServerName { get; set; }

        /// <summary>
        /// Name of target database
        /// </summary>
        public string TargetDatabaseName { get; set; }

        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare publish changes request type
    /// </summary>
    class SchemaComparePublishChangesRequest
    {
        public static readonly RequestType<SchemaComparePublishChangesParams, ResultStatus> Type =
            RequestType<SchemaComparePublishChangesParams, ResultStatus>.Create("schemaCompare/publish");
    }
}
