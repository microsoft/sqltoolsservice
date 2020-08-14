//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Start Profiling request parameters
    /// </summary>
    public class GetXEventSessionsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    public class GetXEventSessionsResult
    {
        /// <summary>
        /// List of XE session names
        /// </summary>
        public List<string> Sessions { get; set; }
    }

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class GetXEventSessionsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<GetXEventSessionsParams, GetXEventSessionsResult> Type =
            RequestType<GetXEventSessionsParams, GetXEventSessionsResult>.Create("profiler/getsessions");
    }
}
