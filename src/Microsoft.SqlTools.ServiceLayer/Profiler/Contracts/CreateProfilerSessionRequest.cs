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
    public class CreateProfilerSessionParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string CreateStatement {get; set; }

        public string SessionName {get; set; }
    }

    public class CreateProfilerSessionResult
    {
        /// <summary>
        /// Session ID that was started
        /// </summary>
        public string SessionId { get; set; }

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class CreateProfilerSessionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateProfilerSessionParams, CreateProfilerSessionResult> Type =
            RequestType<CreateProfilerSessionParams, CreateProfilerSessionResult>.Create("profiler/createsession");
    }
}
