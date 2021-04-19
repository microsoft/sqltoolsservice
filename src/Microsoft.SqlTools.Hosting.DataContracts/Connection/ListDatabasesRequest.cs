using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Connection
{
    public class ListDatabasesRequest
    {
        public static readonly
            RequestType<ListDatabasesParams, ListDatabasesResponse> Type =
                RequestType<ListDatabasesParams, ListDatabasesResponse>.Create("connection/listdatabases");
    }
}