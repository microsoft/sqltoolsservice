//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx generate deploy script request.
    /// </summary>
    public class GenerateDeployScriptParams : DacFxParams
    {
        /// <summary>
        /// Gets or sets the filepath where to save the generated script
        /// </summary>
        public string ScriptFilePath { get; set; }
    }

    /// <summary>
    /// Defines the DacFx generate deploy script request type
    /// </summary>
    class GenerateDeployScriptRequest
    {
        public static readonly RequestType<GenerateDeployScriptParams, DacFxResult> Type =
            RequestType<GenerateDeployScriptParams, DacFxResult>.Create("dacfx/generateUgradeScript");
    }
}
