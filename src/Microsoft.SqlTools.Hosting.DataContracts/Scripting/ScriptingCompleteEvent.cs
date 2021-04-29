using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting
{
    public class ScriptingCompleteEvent
    {
        public static readonly EventType<ScriptingCompleteParams> Type = 
            EventType<ScriptingCompleteParams>.Create("scripting/scriptComplete");
    }
}