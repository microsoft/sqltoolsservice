using Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting
{
    public class ScriptingListObjectsRequest
    {
        public static readonly RequestType<ScriptingListObjectsParams, ScriptingListObjectsResult> Type = 
            RequestType<ScriptingListObjectsParams, ScriptingListObjectsResult>.Create("scripting/listObjects");
    }
}