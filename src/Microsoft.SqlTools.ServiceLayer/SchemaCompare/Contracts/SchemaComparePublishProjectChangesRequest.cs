//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Dac.Compare;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare publish project changes request.
    /// Extends SqlCore's version with TaskExecutionMode.
    /// </summary>
    public class SchemaComparePublishProjectChangesParams : CoreContracts.SchemaComparePublishProjectChangesParams
    {
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