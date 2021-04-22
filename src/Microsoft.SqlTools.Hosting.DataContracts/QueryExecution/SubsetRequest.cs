using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class SubsetRequest
    {
        public static readonly
            RequestType<SubsetParams, SubsetResult> Type = RequestType<SubsetParams, SubsetResult>.Create("query/subset");
    }
}