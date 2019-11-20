//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;

namespace Microsoft.Kusto.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Stop Profiling request parameters
    /// </summary>
    public class StopProfilingParams
    {
        public string OwnerUri { get; set; }
    }

    public class StopProfilingResult{}

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class StopProfilingRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<StopProfilingParams, StopProfilingResult> Type =
            RequestType<StopProfilingParams, StopProfilingResult>.Create("profiler/stop");
    }
}
