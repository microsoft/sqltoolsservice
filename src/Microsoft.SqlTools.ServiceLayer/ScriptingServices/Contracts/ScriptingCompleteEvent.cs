//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Parameters to be sent to a indicate a scripting operations has completed.
    /// </summary>
    public class ScriptingCompleteParameters : ScriptingEventParams {}


    /// <summary>
    /// Event sent to a indicate a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteEvent
    {
        public static readonly EventType<ScriptingCompleteParameters> Type = EventType<ScriptingCompleteParameters>.Create("scripting/scriptComplete");
    }
}
