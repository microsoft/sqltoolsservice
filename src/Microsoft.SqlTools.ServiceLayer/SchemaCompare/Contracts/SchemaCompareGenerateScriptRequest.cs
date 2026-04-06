//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare generate script request.
    /// Extends SqlCore's version with TaskExecutionMode.
    /// </summary>
    public class SchemaCompareGenerateScriptParams : CoreContracts.SchemaCompareGenerateScriptParams
    {
        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare generate script request type
    /// </summary>
    class SchemaCompareGenerateScriptRequest
    {
        public static readonly RequestType<SchemaCompareGenerateScriptParams, ResultStatus> Type =
            RequestType<SchemaCompareGenerateScriptParams, ResultStatus>.Create("schemaCompare/generateScript");
    }
}
