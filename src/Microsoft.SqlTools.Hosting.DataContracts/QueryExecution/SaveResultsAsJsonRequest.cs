using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    /// <summary>
    /// Request type to save results as JSON
    /// </summary>
    public class SaveResultsAsJsonRequest
    {
        public static readonly
            RequestType<SaveResultsAsJsonRequestParams, SaveResultRequestResult> Type =
                RequestType<SaveResultsAsJsonRequestParams, SaveResultRequestResult>.Create("query/saveJson");
    }
}