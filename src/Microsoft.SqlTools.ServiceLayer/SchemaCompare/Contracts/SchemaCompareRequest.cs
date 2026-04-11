// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Info needed from endpoints for schema comparison.
    /// Extends the SqlCore host-agnostic type with ConnectionDetails, which is
    /// VSCode/ServiceLayer-specific and used by VsCodeConnectionProvider.
    /// SSMS, which references SqlCore directly as a NuGet package, will use
    /// CoreContracts.SchemaCompareEndpointInfo directly and never need this subtype.
    /// </summary>
    public class SchemaCompareEndpointInfo : CoreContracts.SchemaCompareEndpointInfo
    {
        /// <summary>
        /// Connection details (VSCode/ServiceLayer-specific; not used by SqlCore operations)
        /// </summary>
        public ConnectionDetails ConnectionDetails { get; set; }
    }

    /// <summary>
    /// Parameters for a schema compare request.
    /// Extends the SqlCore host-agnostic type with TaskExecutionMode, which is
    /// ServiceLayer-specific and used by SqlTaskManager.
    /// SSMS will use CoreContracts.SchemaCompareParams directly.
    /// </summary>
    public class SchemaCompareParams : CoreContracts.SchemaCompareParams
    {
        /// <summary>
        /// Execution mode for the operation. Default is execution.
        /// ServiceLayer-specific; not used by SqlCore operations.
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }
    }

    /// <summary>
    /// Parameters returned from a schema compare request.
    /// </summary>
    public class SchemaCompareResult : ResultStatus
    {
        public string OperationId { get; set; }

        public bool AreEqual { get; set; }

        public List<DiffEntry> Differences { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare request type
    /// </summary>
    class SchemaCompareRequest
    {
        public static readonly RequestType<SchemaCompareParams, SchemaCompareResult> Type =
            RequestType<SchemaCompareParams, SchemaCompareResult>.Create("schemaCompare/compare");
    }
}