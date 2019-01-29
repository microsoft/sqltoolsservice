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
    /// Parameters for a DacFx generate deploy plan request.
    /// </summary>
    public class GenerateDeployPlanParams : DacFxParams
    {
    }

    /// <summary>
    /// Defines the DacFx generate deploy plan request type
    /// </summary>
    class GenerateDeployPlanRequest
    {
        public static readonly RequestType<GenerateDeployPlanParams, GenerateDeployPlanRequestResult> Type =
            RequestType<GenerateDeployPlanParams, GenerateDeployPlanRequestResult>.Create("dacfx/generateDeployPlan");
    }

    /// <summary>
    /// Parameters returned from a generate deploy script request.
    /// </summary>
    public class GenerateDeployPlanRequestResult : DacFxResult
    {
        public string Report { get; set; }
    }
}
