using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    /// <summary>
    /// Request type to save results as Excel
    /// </summary>
    public class SaveResultsAsExcelRequest
    {
        public static readonly
            RequestType<SaveResultsAsExcelRequestParams, SaveResultRequestResult> Type =
                RequestType<SaveResultsAsExcelRequestParams, SaveResultRequestResult>.Create("query/saveExcel");
    }
}