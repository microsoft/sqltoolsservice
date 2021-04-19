using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    public class DisconnectRequest
    {
        public static readonly
            RequestType<DisconnectParams, bool> Type =
                RequestType<DisconnectParams, bool>.Create("connection/disconnect");
    }
}