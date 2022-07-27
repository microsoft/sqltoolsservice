//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlServer.Dac.Model;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters to generate a SQL model
    /// </summary>
    public class GenerateTSqlModelParams
    {
        /// <summary>
        /// Project uri
        /// </summary>
        public string ProjectUri { get; set; }

        /// <summary>
        /// The version of Sql Server to target
        /// </summary>
        public string ModelTargetVersion { get; set; }

        /// <summary>
        /// Gets or sets the Sql script file paths.
        /// </summary>
        public string[]? FilePaths { get; set; }
    }

    /// <summary>
    /// Result for the GenerateTSqlModel Request.
    /// </summary>
    public class GenerateTSqlModelResult : ResultStatus
    {
        public TSqlModel Model { get; set; }

        public GenerateTSqlModelResult(TSqlModel model)
        {
            this.Model = model;
        }
    }

    /// <summary>
    /// Defines the generate sql model request
    /// </summary>
    class GenerateTSqlModelRequest
    {
        public static readonly RequestType<GenerateTSqlModelParams, GenerateTSqlModelResult> Type =
            RequestType<GenerateTSqlModelParams, GenerateTSqlModelResult>.Create("dacFx/generateTSqlModel");
    }
}
