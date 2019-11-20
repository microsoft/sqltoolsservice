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
    /// Pause Profiling request parameters
    /// </summary>
    public class PauseProfilingParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    public class PauseProfilingResult{}

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
