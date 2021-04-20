using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class QueryCompleteEvent
    {
        public static readonly EventType<QueryCompleteParams> Type = EventType<QueryCompleteParams>.Create("query/complete");
    }
}