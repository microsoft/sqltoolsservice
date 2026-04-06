//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using System.Collections.Generic;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare include specific node request.
    /// Extends SqlCore's version with TaskExecutionMode.
    /// </summary>
    public class SchemaCompareNodeParams : CoreContracts.SchemaCompareNodeParams
    {
        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    class SchemaCompareIncludeExcludeNodeRequest
    {
        public static readonly RequestType<SchemaCompareNodeParams, ResultStatus> Type =
       RequestType<SchemaCompareNodeParams, ResultStatus>.Create("schemaCompare/includeExcludeNode");
    }

    /// <summary>
    /// Parameters returned from a schema compare include/exclude request.
    /// </summary>
    public class SchemaCompareIncludeExcludeResult : ResultStatus
    {
        /// <summary>
        /// Dependencies that may have been affected by the include/exclude request
        /// </summary>
        public List<DiffEntry> AffectedDependencies { get; set; }

        /// <summary>
        /// Dependencies that caused the include/exclude to fail
        /// </summary>
        public List<DiffEntry> BlockingDependencies { get; set; }

    }
}
