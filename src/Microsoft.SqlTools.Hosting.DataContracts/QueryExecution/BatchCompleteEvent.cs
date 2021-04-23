using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class BatchCompleteEvent
    {
        public static readonly 
            EventType<BatchEventParams> Type =
                EventType<BatchEventParams>.Create("query/batchComplete");
    }
}