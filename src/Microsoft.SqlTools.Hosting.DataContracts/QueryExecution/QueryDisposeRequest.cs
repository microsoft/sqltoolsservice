using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class QueryDisposeRequest
    {
        public static readonly
            RequestType<QueryDisposeParams, QueryDisposeResult> Type =
                RequestType<QueryDisposeParams, QueryDisposeResult>.Create("query/dispose");
    }
}