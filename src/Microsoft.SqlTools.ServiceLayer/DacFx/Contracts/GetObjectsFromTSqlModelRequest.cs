//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters to generate a SQL model
    /// </summary>
    public class GetObjectsFromTSqlModelParams
    {
        /// <summary>
        /// URI of the project file this model is for
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// Object types to query
        /// </summary>
        public string[] ObjectTypes { get; set; }
    }

    /// <summary>
    /// Defines the generate sql model request
    /// </summary>
    class GetObjectsFromTSqlModelRequest
    {
        public static readonly RequestType<GenerateTSqlModelParams, ResultStatus> Type =
            RequestType<GenerateTSqlModelParams, ResultStatus>.Create("dacFx/getObjectsFromTSqlModel");
    }
}
