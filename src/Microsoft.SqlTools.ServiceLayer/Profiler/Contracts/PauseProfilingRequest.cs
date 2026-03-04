//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Pause Profiling request parameters
    /// </summary>
    public class PauseProfilingParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Pause Profiling result
    /// </summary>
    public class PauseProfilingResult
    {
        /// <summary>
        /// Indicates whether the event session is currently paused after the toggle operation
        /// </summary>
        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// Pause Profile request type
    /// </summary>
    public class PauseProfilingRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<PauseProfilingParams, PauseProfilingResult> Type =
            RequestType<PauseProfilingParams, PauseProfilingResult>.Create("profiler/pause");
    }
}
