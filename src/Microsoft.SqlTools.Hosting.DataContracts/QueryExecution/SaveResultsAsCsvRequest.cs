using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    /// <summary>
    /// Request type to save results as CSV
    /// </summary>
    public class SaveResultsAsCsvRequest
    {
        public static readonly
            RequestType<SaveResultsAsCsvRequestParams, SaveResultRequestResult> Type =
                RequestType<SaveResultsAsCsvRequestParams, SaveResultRequestResult>.Create("query/saveCsv");
    }
}