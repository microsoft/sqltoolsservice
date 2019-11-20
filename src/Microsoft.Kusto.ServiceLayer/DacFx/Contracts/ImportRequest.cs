//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.TaskServices;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.DacFx.Contracts
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
