//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>P
    /// Start Profiling request parameters
    /// </summary>
    public class ListAvailableSessionsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    public class ListAvailableSessionsResult
    {
        /// <summary>
        /// List of all XEvent sessions available
        /// </summary>
        public List<string> AvailableSessions { get; set; }

        public bool Succeeded { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Start Profile request type
    /// </summary>
    public class ListAvailableSessionsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<ListAvailableSessionsParams, ListAvailableSessionsResult> Type =
            RequestType<ListAvailableSessionsParams, ListAvailableSessionsResult>.Create("profiler/listavailablesessions");
    }
}
