//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.Contracts
{
    public class GetLiveExecutionPlanParams
    {
        /// <summary>
        /// URI of the editor whose connection is running the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Server session id (SPID) running the query
        /// </summary>
        public int SessionId { get; set; }
    }

    /// <summary>
    /// Reads the in-flight plan of a running query, with the actual row counts so far. Every read for
    /// an editor reuses one monitoring connection, separate from the connection running the query.
    /// </summary>
    public class GetLiveExecutionPlanRequest
    {
        public static readonly
        RequestType<GetLiveExecutionPlanParams, GetExecutionPlanResult> Type =
         RequestType<GetLiveExecutionPlanParams, GetExecutionPlanResult>.Create("queryExecutionPlan/getLiveExecutionPlan");
    }

    public class EndLiveExecutionPlanParams
    {
        /// <summary>
        /// URI of the editor whose monitoring connection to close
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Closes the monitoring connection opened by getLiveExecutionPlan. Returns false when none is open.
    /// </summary>
    public class EndLiveExecutionPlanRequest
    {
        public static readonly
        RequestType<EndLiveExecutionPlanParams, bool> Type =
         RequestType<EndLiveExecutionPlanParams, bool>.Create("queryExecutionPlan/endLiveExecutionPlan");
    }
}
