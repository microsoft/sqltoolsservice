//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare include specific node request
    /// </summary>
    public class SchemaCompareIncludeExcludeAllNodesParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Indicator for include or exclude request
        /// </summary>
        public bool IncludeRequest { get; set; }

        /// <summary>
        /// Execution mode for the operation. Default is execution
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    class SchemaCompareIncludeExcludeAllNodesRequest
    {
        public static readonly RequestType<SchemaCompareIncludeExcludeAllNodesParams, SchemaCompareIncludeExcludeAllNodesResult> Type =
       RequestType<SchemaCompareIncludeExcludeAllNodesParams, SchemaCompareIncludeExcludeAllNodesResult>.Create("schemaCompare/includeExcludeAllNodes");
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
