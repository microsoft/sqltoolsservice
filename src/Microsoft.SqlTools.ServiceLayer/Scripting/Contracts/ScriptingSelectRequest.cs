//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// The type of scripting operation requested
    /// </summary>
    public enum ScriptOperation
    {
        Select = 0,
        Create = 1,
        Insert = 2,
        Update = 3,
        Delete = 4
    }

    /// <summary>
    /// Script as request parameter type
    /// </summary>
    public class ScriptingScriptAsParams 
    {
        public string OwnerUri { get; set; }

        public ScriptOperation Operation { get; set; }

        public ObjectMetadata Metadata { get; set; }
    }

    /// <summary>
    /// Script as request result type
    /// </summary>
    public class ScriptingScriptAsResult
    {
        public string OwnerUri { get; set; }

        public string Script { get; set; }
    }

    /// <summary>
    /// Script as request message type
    /// </summary>
    public class ScriptingScriptAsRequest
    {
        public static readonly
            RequestType<ScriptingScriptAsParams, ScriptingScriptAsResult> Type =
                RequestType<ScriptingScriptAsParams, ScriptingScriptAsResult>.Create("scripting/scriptas");
    }
}