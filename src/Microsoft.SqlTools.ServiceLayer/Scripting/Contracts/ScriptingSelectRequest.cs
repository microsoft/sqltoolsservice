//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters for a select scripting request.
    /// </summary>
    public class ScriptingSelectParams
    {
        /// <summary>
        /// Gets or sets connection string of the target database the scripting operation will run against.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a scripting object to script.
        /// </summary>
        public ScriptingObject ScriptingObject { get; set; }
    }

    /// <summary>
    /// Parameters returned from a select scripting request.
    /// </summary>
    public class ScriptingSelectResult
    {   
        /// <summary>
        /// The returned select script string returned from the service
        /// </summary>
        public string script;
    }
    public class ScriptingSelectRequest
    {
        public static RequestType<ScriptingSelectParams, ScriptingSelectResult> Type =
            RequestType<ScriptingSelectParams, ScriptingSelectResult>.Create("scripting/scriptselect");
    }
}