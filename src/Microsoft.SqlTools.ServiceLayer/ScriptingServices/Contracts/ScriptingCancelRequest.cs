//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Parameters for the script database request
    /// </summary>
    public class ScriptingCancelParams
    {
        public string OperationId { get; set; }
    }

    /// <summary>
    /// Parameters for the script database result
    /// </summary>
    public class ScriptingCancelResult { }

    public class ScriptingCancelRequest
    {
        public static readonly RequestType<ScriptingCancelParams, ScriptingCancelResult> Type = 
            RequestType<ScriptingCancelParams, ScriptingCancelResult>.Create("scripting/scriptCancel");
    }
}
