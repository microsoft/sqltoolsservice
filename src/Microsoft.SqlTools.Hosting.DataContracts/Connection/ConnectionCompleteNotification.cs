using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    /// <summary>
    /// ConnectionComplete notification mapping entry 
    /// </summary>
    public class ConnectionCompleteNotification
    {
        public static readonly EventType<ConnectionCompleteParams> Type = EventType<ConnectionCompleteParams>.Create("connection/complete");
    }
}