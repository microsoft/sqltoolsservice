//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a Validate Streaming Job request.
    /// </summary>
    public class ValidateStreamingJobParams
    {
        /// <summary>
        /// Gets or sets the package file path
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// Gets or sets the create streaming job TSQL.  Should not be used if Statement is set.
        /// </summary>
        public string CreateStreamingJobTsql { get; set;}
    }

    /// <summary>
    /// Parameters returned from a DacFx validate streaming job request.
    /// </summary>
    public class ValidateStreamingJobResult : ResultStatus
    {
        
    }

    /// <summary>
    /// Defines the DacFx validate streaming job request type
    /// </summary>
    class ValidateStreamingJobRequest
    {
        public static readonly RequestType<ValidateStreamingJobParams, ValidateStreamingJobResult> Type =
            RequestType<ValidateStreamingJobParams, ValidateStreamingJobResult>.Create("dacfx/validateStreamingJob");
    }
}
