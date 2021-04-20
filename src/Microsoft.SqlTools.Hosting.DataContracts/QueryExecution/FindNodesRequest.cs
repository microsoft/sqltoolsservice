using Microsoft.SqlTools.Hosting.DataContracts.QueryExecution.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.QueryExecution
{
    public class FindNodesRequest
    {
        public static readonly
            RequestType<FindNodesParams, FindNodesResponse> Type =
                RequestType<FindNodesParams, FindNodesResponse>.Create("objectexplorer/findnodes");
    }
}