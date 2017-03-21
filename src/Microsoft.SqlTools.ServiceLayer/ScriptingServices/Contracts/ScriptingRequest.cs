//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Parameters for a script request
    /// </summary>
    public class ScriptingParams
    {
        public string FilePath { get; set; }

        public string ConnectionString { get; set; }

        public List<ScriptingObject> DatabaseObjects { get; set; }

        public ScriptOptions ScriptOptions { get; set; }
    }

    /// <summary>
    /// Parameters for the script result
    /// </summary>
    public class ScriptingResult
    {
        public string OperationId { get; set; }
    }

    public class ScriptingRequest
    {
        public static readonly RequestType<ScriptingParams, ScriptingResult> Type = RequestType<ScriptingParams, ScriptingResult>.Create("scripting/script");
    }
}
