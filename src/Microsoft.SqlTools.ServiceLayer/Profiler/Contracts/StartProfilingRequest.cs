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
    public class StartProfilingParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        /// <summary>
        /// For LiveTarget and RingBuffer sessions, the name of the remote session.
        /// For LocalFile sessions, the full path of the XEL file to open.
        /// </summary>
        public string SessionName { get; set; }

        /// <summary>
        /// Identifies which type of target the session name identifies.
        /// </summary>
        public ProfilingSessionType SessionType { get; set; } = ProfilingSessionType.RingBuffer;
    }

    public enum ProfilingSessionType
    {
        RingBuffer,
        LiveTarget,
        LocalFile
    }

    public class StartProfilingResult{}

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class StartProfilingRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<StartProfilingParams, StartProfilingResult> Type =
            RequestType<StartProfilingParams, StartProfilingResult>.Create("profiler/start");
    }
}
