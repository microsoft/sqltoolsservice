//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters returned from a script request.
    /// </summary>
    public class ScriptingResult
    {
        public string OperationId { get; set; }

        public string Script { get; set; }
    }

    /// <summary>
    /// Defines the scripting request type.
    /// </summary>
    public class ScriptingRequest
    {
        public static readonly RequestType<ScriptingParams, ScriptingResult> Type =
            RequestType<ScriptingParams, ScriptingResult>.Create("scripting/script");
    }
}
