using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class SimpleExecuteRequest
    {
        public static readonly
            RequestType<SimpleExecuteParams, SimpleExecuteResult> Type =
                RequestType<SimpleExecuteParams, SimpleExecuteResult>.Create("query/simpleexecute");
    }
}