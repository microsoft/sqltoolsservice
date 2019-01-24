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
    /// Parameters for a DacFx upgrade plan request.
    /// </summary>
    public class UpgradePlanParams : DacFxParams
    {
    }

    /// <summary>
    /// Defines the DacFx upgrade plan request type
    /// </summary>
    class UpgradePlanRequest
    {
        public static readonly RequestType<UpgradePlanParams, UpgradePlanRequestResult> Type =
            RequestType<UpgradePlanParams, UpgradePlanRequestResult>.Create("dacfx/upgradePlan");
    }

    /// <summary>
    /// Parameters returned from a generate deploy script request.
    /// </summary>
    public class UpgradePlanRequestResult : DacFxResult
    {
        public string Report { get; set; }
    }
}
