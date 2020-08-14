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
        /// For RemoteSession sessions, the name of the remote session.
        /// For LocalFile sessions, the full path of the XEL file to open.
        /// </summary>
        public string SessionName { get; set; }

        /// <summary>
        /// Identifies which type of target the session name identifies.
        /// </summary>
        public ProfilingSessionType SessionType { get; set; } = ProfilingSessionType.RemoteSession;
    }

    public enum ProfilingSessionType
    {
        RemoteSession,
        LocalFile
    }

    /// <summary>
    /// Provides information about the session that was started
    /// </summary>
    public class StartProfilingResult
    {
        /// <summary>
        /// A unique key to identify the session
        /// </summary>
        public string UniqueSessionId { get; set; }

        /// <summary>
        /// Whether the profiling session supports the Pause operation.
        /// </summary>
        public bool CanPause { get; set; }
    }

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
