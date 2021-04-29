using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting
{
    public class ScriptingRequest
    {
        public static readonly RequestType<ScriptingParams, ScriptingResult> Type =
            RequestType<ScriptingParams, ScriptingResult>.Create("scripting/script");
    }
}