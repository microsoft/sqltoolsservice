//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters sent when a scripting operation has encountered an error.
    /// </summary>
    public class ScriptingErrorParams : ScriptingEventParams
    {
        /// <summary>
        /// Gets or sets error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets error details.
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// Event sent to indicate a scripting operation has encountered an error.
    /// </summary>
    public class ScriptingErrorEvent
    {
        public static readonly EventType<ScriptingErrorParams> Type = 
            EventType<ScriptingErrorParams>.Create("scripting/scriptError");
    }
}
