// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters to return when a result set is started or completed
    /// </summary>
    public class ResultSetEventParams
    {
        public ResultSetSummary ResultSetSummary { get; set; }

        public string OwnerUri { get; set; }
    }

    public class ResultSetCompleteEvent
    {
        public static readonly
            EventType<ResultSetEventParams> Type =
            EventType<ResultSetEventParams>.Create("query/resultSetComplete");
    }

    public class ResultSetAvailableEvent
    {
        public static readonly
            EventType<ResultSetEventParams> Type =
            EventType<ResultSetEventParams>.Create("query/resultSetAvailable");
    }

    public class ResultSetUpdatedEvent
    {
        public static readonly
            EventType<ResultSetEventParams> Type =
            EventType<ResultSetEventParams>.Create("query/resultSetUpdated");
    }

}
