//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters to generate a SQL model
    /// </summary>
    public class GenerateTSqlModelParams
    {
        /// <summary>
        /// URI of the project file this model is for
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// The version of Sql Server to target
        /// </summary>
        public string ModelTargetVersion { get; set; }

        /// <summary>
        /// Gets or sets the Sql script file paths.
        /// </summary>
        public string[] FilePaths { get; set; }
    }

    /// <summary>
    /// Defines the generate sql model request
    /// </summary>
    class GenerateTSqlModelRequest
    {
        public static readonly RequestType<GenerateTSqlModelParams, bool> Type =
            RequestType<GenerateTSqlModelParams, bool>.Create("dacFx/generateTSqlModel");
    }
}
