using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ResultSetCompleteEvent
    {
        public static string MethodName { get; } = "query/resultSetComplete";

        public static readonly
            EventType<ResultSetCompleteEventParams> Type =
                EventType<ResultSetCompleteEventParams>.Create(MethodName);
    }
}