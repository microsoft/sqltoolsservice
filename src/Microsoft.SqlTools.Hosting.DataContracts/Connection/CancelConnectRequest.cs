using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    /// <summary>
    /// Cancel connect request mapping entry 
    /// </summary>
    public class CancelConnectRequest
    {
        public static readonly
            RequestType<CancelConnectParams, bool> Type = RequestType<CancelConnectParams, bool>.Create("connection/cancelconnect");
    }
}