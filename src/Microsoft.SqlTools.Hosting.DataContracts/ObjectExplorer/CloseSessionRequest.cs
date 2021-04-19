using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class CloseSessionRequest
    {
        public static readonly
            RequestType<CloseSessionParams, CloseSessionResponse> Type =
                RequestType<CloseSessionParams, CloseSessionResponse>.Create("objectexplorer/closesession");
    }
}