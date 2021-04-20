using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ExecuteStringRequest
    {
        public static readonly 
            RequestType<ExecuteStringParams, ExecuteRequestResult> Type = 
                RequestType<ExecuteStringParams, ExecuteRequestResult>.Create("query/executeString");
    }
}