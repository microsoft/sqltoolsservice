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
    /// Parameters for a DacFx export request.
    /// </summary>
    public class ExportParams : DacFxParams
    {
    }

    /// <summary>
    /// Defines the DacFx export request type
    /// </summary>
    class ExportRequest
    {
        public static readonly RequestType<ExportParams, DacFxResult> Type =
            RequestType<ExportParams, DacFxResult>.Create("dacfx/export");
    }
}
