//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Parameters to be sent to indicate a scripting operations has encountered and error.
    /// </summary>
    public class ScriptingErrorParams : ScriptingEventParams
    {
        public string Message { get; set; }

        public string DiagnosticMessage { get; set; }
    }

    /// <summary>
    /// Event sent to indicate a scripting database operation has encountered an error.
    /// </summary>
    public class ScriptingErrorEvent
    {
        public static readonly EventType<ScriptingErrorParams> Type = 
            EventType<ScriptingErrorParams>.Create("scripting/scriptError");
    }
}
