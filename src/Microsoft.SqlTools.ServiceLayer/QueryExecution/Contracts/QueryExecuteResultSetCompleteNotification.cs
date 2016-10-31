// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    public class QueryExecuteResultSetCompleteParams
    {
        public ResultSetSummary ResultSetSummary { get; set; }

        public string OwnerUri { get; set; }
    }

    public class QueryExecuteResultSetCompleteEvent
    {
        public static readonly
            EventType<QueryExecuteBatchCompleteParams> Type =
            EventType<QueryExecuteBatchCompleteParams>.Create("query/resultSetComplete");
    }
}
