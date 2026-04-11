// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using CoreContracts = Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    /// <summary>
    /// Parameters for a schema compare cancel request.
    /// Extends CoreContracts version - no additional ServiceLayer-specific fields needed.
    /// </summary>
    public class SchemaCompareCancelParams : CoreContracts.SchemaCompareCancelParams
    {
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