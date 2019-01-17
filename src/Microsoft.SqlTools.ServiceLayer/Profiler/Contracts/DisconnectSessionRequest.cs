//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Disconnect Session request parameters
    /// </summary>
    public class DisconnectSessionParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    public class DisconnectSessionResult { }

    /// <summary>
    /// Disconnect session request type
    /// </summary>
    public class DisconnectSessionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DisconnectSessionParams, DisconnectSessionResult> Type =
            RequestType<DisconnectSessionParams, DisconnectSessionResult>.Create("profiler/disconnect");
    }
}
