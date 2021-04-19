using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    public class ChangeDatabaseRequest
    {
        public static readonly
            RequestType<ChangeDatabaseParams, bool> Type = RequestType<ChangeDatabaseParams, bool>.Create("connection/changedatabase");
    }
}