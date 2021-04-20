using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class ExecuteDocumentSelectionRequest
    {
        public static readonly
            RequestType<ExecuteDocumentSelectionParams, ExecuteRequestResult> Type =
                RequestType<ExecuteDocumentSelectionParams, ExecuteRequestResult>.Create("query/executeDocumentSelection");
    }
}