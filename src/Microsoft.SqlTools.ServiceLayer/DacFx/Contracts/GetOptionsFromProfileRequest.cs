//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx get options from profile request.
    /// </summary>
    public class GetOptionsFromProfileParams
    {
        /// <summary>
        /// Gets or sets the profile path
        /// </summary>
        public string ProfilePath { get; set; }
    }

    /// <summary>
    /// Parameters returned from a DacFx get options from profile request.
    /// </summary>
    public class DacFxOptionsResult : ResultStatus
    {
        public DeploymentOptions DeploymentOptions { get; set; }
    }

    /// <summary>
    /// Defines the DacFx get options from profile request type
    /// </summary>
    class GetOptionsFromProfileRequest
    {
        public static readonly RequestType<GetOptionsFromProfileParams, DacFxOptionsResult> Type =
            RequestType<GetOptionsFromProfileParams, DacFxOptionsResult>.Create("dacfx/getOptionsFromProfile");
    }
}
