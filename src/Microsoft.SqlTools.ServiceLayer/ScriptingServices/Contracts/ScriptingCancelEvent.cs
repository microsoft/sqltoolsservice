//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ScriptingServices.Contracts
{
    /// <summary>
    /// Parameters to be sent to a indicate a scripting operations has been canceled.
    /// </summary>
    public class ScriptingCancelParameters : ScriptingEventParams { }

    /// <summary>
    /// Event sent to a indicate that a scripting operation has been canceled.
    /// </summary>
    public class ScriptingCancelEvent
    {
        public static readonly EventType<ScriptingCancelParameters> Type = EventType<ScriptingCancelParameters>.Create("scripting/scriptCancel");
    }
}
