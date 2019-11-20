//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Start Profiling request parameters
    /// </summary>
    public class StartProfilingParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string SessionName { get; set; }
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
