// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Base class of parameters to return when a result set is available, updated or completed
    /// </summary>
    public abstract class ResultSetEventParams
    {
        public ResultSetSummary ResultSetSummary { get; set; }

        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Parameters to return when a result set is completed.
    /// </summary>
    public class ResultSetCompleteEventParams : ResultSetEventParams
    {
    }

    /// <summary>
    /// Parameters to return when a result set is available.
    /// </summary>
    public class ResultSetAvailableEventParams : ResultSetEventParams
    {
    }

    /// <summary>
    /// Parameters to return when a result set is updated
    /// </summary>
    public class ResultSetUpdatedEventParams : ResultSetEventParams
    {
    }

    public class ResultSetCompleteEvent 
    {
        public static string MethodName { get; } = "query/resultSetComplete";

        public static readonly
            EventType<ResultSetCompleteEventParams> Type =
            EventType<ResultSetCompleteEventParams>.Create(MethodName);
    }

    public class ResultSetAvailableEvent 
    {
        public static string MethodName { get; } = "query/resultSetAvailable";

        public static readonly
            EventType<ResultSetAvailableEventParams> Type =
            EventType<ResultSetAvailableEventParams>.Create(MethodName);
    }

    public class ResultSetUpdatedEvent 
    {
        public static string MethodName { get; } = "query/resultSetUpdated";

        public static readonly
            EventType<ResultSetUpdatedEventParams> Type =
            EventType<ResultSetUpdatedEventParams>.Create(MethodName);
    }

}
