using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ResultSetAvailableEvent
    {
        public static string MethodName { get; } = "query/resultSetAvailable";

        public static readonly
            EventType<ResultSetAvailableEventParams> Type =
                EventType<ResultSetAvailableEventParams>.Create(MethodName);
    }
}