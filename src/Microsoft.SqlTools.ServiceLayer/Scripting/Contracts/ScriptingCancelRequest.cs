﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters to cancel a scripting request.
    /// </summary>
    public class ScriptingCancelParams : ScriptingEventParams { }

    /// <summary>
    /// Parameters returned from a scripting request.
    /// </summary>
    public class ScriptingCancelResult { }

    /// <summary>
    /// Defines the scripting cancel request type.
    /// </summary>
    public class ScriptingCancelRequest
    {
        public static readonly RequestType<ScriptingCancelParams, ScriptingCancelResult> Type = 
            RequestType<ScriptingCancelParams, ScriptingCancelResult>.Create("scripting/scriptCancel");
    }
}
