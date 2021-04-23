using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    /// <summary>
    /// Request type to save results as XML
    /// </summary>
    public class SaveResultsAsXmlRequest
    {
        public static readonly
            RequestType<SaveResultsAsXmlRequestParams, SaveResultRequestResult> Type =
                RequestType<SaveResultsAsXmlRequestParams, SaveResultRequestResult>.Create("query/saveXml");
    }
}