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
    /// Parameters for a schema compare include specific node request
    /// </summary>
    public class SchemaCompareNodeParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Difference to Include or exclude
        /// </summary>
        public DiffEntry DiffEntry { get; set; }  
        
        /// <summary>
        /// Indicator for include or exclude request
        /// </summary>
        public bool IncludeRequest { get; set; }

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
}
