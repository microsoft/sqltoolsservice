using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    /// <summary>
    /// Connect request mapping entry 
    /// </summary>
    public class ConnectionRequest
    {
        public static readonly RequestType<ConnectParams, bool> Type = RequestType<ConnectParams, bool>.Create("connection/connect");
    }
}