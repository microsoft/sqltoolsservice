//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare publish database changes request.
    /// </summary>
    public class SchemaComparePublishDatabaseChangesParams
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
    /// Defines the Schema Compare publish database changes request type
    /// </summary>
    class SchemaComparePublishDatabaseChangesRequest
    {
        public static readonly RequestType<SchemaComparePublishDatabaseChangesParams, ResultStatus> Type =
            RequestType<SchemaComparePublishDatabaseChangesParams, ResultStatus>.Create("schemaCompare/publishDatabase");
    }
}
