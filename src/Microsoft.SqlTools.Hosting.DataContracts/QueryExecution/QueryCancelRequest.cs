using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class QueryCancelRequest
    {
        public static readonly 
            RequestType<QueryCancelParams, QueryCancelResult> Type = RequestType<QueryCancelParams, QueryCancelResult>.Create("query/cancel");
    }
}