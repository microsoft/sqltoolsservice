using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ResultSetUpdatedEvent
    {
        public static string MethodName { get; } = "query/resultSetUpdated";

        public static readonly
            EventType<ResultSetUpdatedEventParams> Type =
                EventType<ResultSetUpdatedEventParams>.Create(MethodName);
    }
}