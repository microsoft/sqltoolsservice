using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting
{
    public class ScriptingPlanNotificationEvent
    {
        public static readonly EventType<ScriptingPlanNotificationParams> Type = EventType<ScriptingPlanNotificationParams>.Create("scripting/scriptPlanNotification");
    }
}