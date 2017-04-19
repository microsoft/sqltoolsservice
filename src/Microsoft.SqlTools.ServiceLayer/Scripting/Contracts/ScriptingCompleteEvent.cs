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
    public class ScriptingCompleteParameters : ScriptingEventParams {}


    /// <summary>
    /// Event sent to indicate a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteEvent
    {
        public static readonly EventType<ScriptingCompleteParameters> Type = 
            EventType<ScriptingCompleteParameters>.Create("scripting/scriptComplete");
    }
}
