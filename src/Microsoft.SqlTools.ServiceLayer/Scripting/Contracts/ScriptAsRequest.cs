//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{

    /// <summary>
    /// Script as request message type
    /// </summary>
    public class ScriptingScriptAsRequest
    {
        public static readonly
            RequestType<ScriptingParams, ScriptingResult> Type =
                RequestType<ScriptingParams, ScriptingResult>.Create("scripting/scriptas");
    }
}
