using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting
{
    public class ScriptingCancelRequest
    {
        public static readonly RequestType<ScriptingCancelParams, ScriptingCancelResult> Type = 
            RequestType<ScriptingCancelParams, ScriptingCancelResult>.Create("scripting/scriptCancel");
    }
}