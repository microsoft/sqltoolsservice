using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    public class ConnectionChangedNotification
    {
        public static readonly
            EventType<ConnectionChangedParams> Type = EventType<ConnectionChangedParams>.Create("connection/connectionchanged");
    }
}