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
    /// Parameters for a schema compare save SCMP request.
    /// Extends SqlCore's version with TaskExecutionMode.
    /// </summary>
    internal class SchemaCompareSaveScmpParams : CoreContracts.SchemaCompareSaveScmpParams
    {
        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    internal class SchemaCompareSaveScmpRequest
    {
        public static readonly RequestType<SchemaCompareSaveScmpParams, ResultStatus> Type =
            RequestType<SchemaCompareSaveScmpParams, ResultStatus>.Create("schemaCompare/saveScmp");
    }

}
