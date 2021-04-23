using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class MessageEvent
    {
        public static readonly
            EventType<MessageParams> Type =
                EventType<MessageParams>.Create("query/message");
    }
}