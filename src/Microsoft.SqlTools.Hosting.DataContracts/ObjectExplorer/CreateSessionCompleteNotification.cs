using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class CreateSessionCompleteNotification
    {
        public static readonly
            EventType<SessionCreatedParameters> Type =
                EventType<SessionCreatedParameters>.Create("objectexplorer/sessioncreated");
    }
}