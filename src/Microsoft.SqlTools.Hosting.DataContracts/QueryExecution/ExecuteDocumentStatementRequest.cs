using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ExecuteDocumentStatementRequest
    {
        public static readonly 
            RequestType<ExecuteDocumentStatementParams, ExecuteRequestResult> Type = 
                RequestType<ExecuteDocumentStatementParams, ExecuteRequestResult>.Create("query/executedocumentstatement");
    }
}