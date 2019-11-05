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
    /// Parameters for a DacFx import request.
    /// </summary>
    public class ImportParams : DacFxParams
    {
    }


    /// <summary>
    /// Defines the DacFx import request type
    /// </summary>
    class ImportRequest
    {
        public static readonly RequestType<ImportParams, DacFxResult> Type =
            RequestType<ImportParams, DacFxResult>.Create("dacfx/import");
    }
}
