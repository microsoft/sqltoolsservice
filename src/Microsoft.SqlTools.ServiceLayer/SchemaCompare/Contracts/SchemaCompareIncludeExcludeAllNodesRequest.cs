//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare include/exclude all nodes request.
    /// Extends SqlCore's version with TaskExecutionMode.
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesParams : CoreContracts.SchemaCompareIncludeExcludeAllNodesParams
    {
        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    public class SchemaCompareIncludeExcludeAllNodesRequest
    {
        public static readonly RequestType<SchemaCompareIncludeExcludeAllNodesParams, ResultStatus> Type =
       RequestType<SchemaCompareIncludeExcludeAllNodesParams, ResultStatus>.Create("schemaCompare/includeExcludeAllNodes");
    }

    /// <summary>
    /// Parameters returned from a schema compare include/exclude all node request.
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesResult : ResultStatus
    {
        /// <summary>
        /// Differences that were all included or excluded
        /// </summary>
        public List<DiffEntry> AllIncludedOrExcludedDifferences { get; set; }

    }
}
