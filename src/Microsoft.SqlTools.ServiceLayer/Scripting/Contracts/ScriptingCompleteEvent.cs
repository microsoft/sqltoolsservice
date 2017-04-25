//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

    using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters sent to when a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteParams : ScriptingEventParams
    {
        public string ErrorDetails { get; set; }

        public string ErrorMessage { get; set; }

        public bool HasError { get; set; }

        public bool Canceled { get; set; }

        public bool Success { get; set; }
    }

    /// <summary>
    /// Event sent to indicate a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteEvent
    {
        public static readonly EventType<ScriptingCompleteParams> Type = 
            EventType<ScriptingCompleteParams>.Create("scripting/scriptComplete");
    }
}
