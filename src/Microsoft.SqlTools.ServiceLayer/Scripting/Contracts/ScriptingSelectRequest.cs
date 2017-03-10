//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    public class ScriptingSelectParams 
    {
        public string OwnerUri { get; set; }        
    }

    public class ScriptingSelectResult
    {
        public string OwnerUri { get; set; }

        public string[] Tables { get; set; }
    }

    public class ScriptingSelectRequest
    {
        public static readonly
            RequestType<ScriptingSelectParams, ScriptingSelectResult> Type =
                RequestType<ScriptingSelectParams, ScriptingSelectResult>.Create("scripting/select");
    }
}
