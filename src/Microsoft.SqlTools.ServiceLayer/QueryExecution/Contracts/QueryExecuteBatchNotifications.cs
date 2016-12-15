// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters to be sent back as part of a QueryExecuteBatchCompleteEvent to indicate that a
    /// batch of a query completed.
    /// </summary>
    public class QueryExecuteBatchNotificationParams
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

    public class QueryExecuteBatchCompleteEvent
    {
        public static readonly 
            EventType<QueryExecuteBatchNotificationParams> Type =
            EventType<QueryExecuteBatchNotificationParams>.Create("query/batchComplete");
    }

    public class QueryExecuteBatchStartEvent
    {
        public static readonly
            EventType<QueryExecuteBatchNotificationParams> Type =
            EventType<QueryExecuteBatchNotificationParams>.Create("query/batchStart");
    }
}
