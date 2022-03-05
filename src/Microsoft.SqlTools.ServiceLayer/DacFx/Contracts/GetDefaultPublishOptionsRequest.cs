//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.DacFx.Contracts
{
    /// <summary>
    /// Parameters for a DacFx get default publish options request.
    /// </summary>
    public class GetDefaultPublishOptionsParams
    {
    }

    /// <summary>
    /// Defines the DacFx get default publish options request type
    /// </summary>
    class GetDefaultPublishOptionsRequest
    {
        public static readonly RequestType<GetDefaultPublishOptionsParams, DacFxOptionsResult> Type =
            RequestType<GetDefaultPublishOptionsParams, DacFxOptionsResult>.Create("dacfx/getDefaultPublishOptions");
    }
}
