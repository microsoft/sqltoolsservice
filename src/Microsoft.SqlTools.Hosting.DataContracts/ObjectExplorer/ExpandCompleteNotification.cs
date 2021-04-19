using Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.ObjectExplorer
{
    public class ExpandCompleteNotification
    {
        public static readonly
            EventType<ExpandResponse> Type =
                EventType<ExpandResponse>.Create("objectexplorer/expandCompleted");
    }
}