//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Start Profiling request parameters
    /// </summary>
    public class CreateXEventSessionParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string SessionName { get; set; }

        public ProfilerSessionTemplate SessionTemplate { get; set; }
    }

    public class CreateXEventSessionResult{}

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class CreateXEventSessionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateXEventSessionParams, CreateXEventSessionResult> Type =
            RequestType<CreateXEventSessionParams, CreateXEventSessionResult>.Create("profiler/createsession");
    }
}
