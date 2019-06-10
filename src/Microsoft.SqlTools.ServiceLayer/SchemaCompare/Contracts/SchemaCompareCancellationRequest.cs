//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare cancel request
    /// </summary>
    public class SchemaCompareCancelParams
    {
        /// <summary>
        /// Operation id of the schema compare operation
        /// </summary>
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Defines the Schema Compare cancel comparison request type
    /// </summary>
    class SchemaCompareCancellationRequest
    {
        public static readonly RequestType<SchemaCompareCancelParams, ResultStatus> Type =
            RequestType<SchemaCompareCancelParams, ResultStatus>.Create("schemaCompare/cancel");
    }
}
