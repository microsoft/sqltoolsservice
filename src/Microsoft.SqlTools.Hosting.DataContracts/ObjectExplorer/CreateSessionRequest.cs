using Microsoft.SqlTools.Hosting.DataContracts.Connection.Models;
using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class CreateSessionRequest
    {
        public static readonly
            RequestType<ConnectionDetails, CreateSessionResponse> Type =
                RequestType<ConnectionDetails, CreateSessionResponse>.Create("objectexplorer/createsession");
    }
}