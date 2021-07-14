//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx extract request.
    /// </summary>
    public class ExtractParams : DacFxParams
    {
        /// <summary>
        /// Gets or sets the string identifier for the DAC application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the version of the DAC application
        /// </summary>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets the target for extraction
        /// </summary>
        public DacExtractTarget ExtractTarget { get; set; }
    }

    /// <summary>
    /// Defines the DacFx extract request type
    /// </summary>
    class ExtractRequest
    {
        public static readonly RequestType<ExtractParams, DacFxResult> Type =
            RequestType<ExtractParams, DacFxResult>.Create("dacfx/extract");
    }
}
