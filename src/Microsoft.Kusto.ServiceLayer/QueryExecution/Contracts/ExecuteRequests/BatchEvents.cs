// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters to be sent back as part of a batch start or complete event to indicate that a
    /// batch of a query started or completed.
    /// </summary>
    public class BatchEventParams
    {
        /// <summary>
        /// Summary of the batch that just completed
        /// </summary>
        public BatchSummary BatchSummary { get; set; }

        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }
    }

    public class BatchCompleteEvent
    {
        public static readonly 
            EventType<BatchEventParams> Type =
            EventType<BatchEventParams>.Create("query/batchComplete");
    }

    public class BatchStartEvent
    {
        public static readonly
            EventType<BatchEventParams> Type =
            EventType<BatchEventParams>.Create("query/batchStart");
    }
}
