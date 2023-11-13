﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters to be sent back with a query execution complete event
    /// </summary>
    public class QueryCompleteParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Summaries of the result sets that were returned with the query
        /// </summary>
        public BatchSummary[] BatchSummaries { get; set; }

        /// <summary>
        /// Process ID of the query that was executed
        /// </summary>
        public string? ServerConnectionId { get; set; }
    }

    public class QueryCompleteEvent
    {
        public static readonly 
            EventType<QueryCompleteParams> Type =
            EventType<QueryCompleteParams>.Create("query/complete");
    }
}
