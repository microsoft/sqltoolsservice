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
        /// The version of Sql Server to target
        /// </summary>
        public string ObjectType { get; set; }
    }

    /// <summary>
    /// Defines the generate sql model request
    /// </summary>
    class GenerateTSqlModelRequest
    {
        public static readonly RequestType<GenerateTSqlModelParams, ResultStatus> Type =
            RequestType<GenerateTSqlModelParams, ResultStatus>.Create("dacFx/generateTSqlModel");
    }
}
