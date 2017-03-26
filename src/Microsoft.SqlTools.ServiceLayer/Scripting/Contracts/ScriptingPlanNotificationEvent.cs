//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters sent to indicate the scripting operation has made progress.
    /// </summary>
    public class ScriptingPlanNotificationParams : ScriptingEventParams
    {
        public List<ScriptingObject> DatabaseObjects { get; set; }

        public int Count { get; set; }
    }

    /// <summary>
    /// Event to indicate the script operation has made progress.
    /// </summary>
    public class ScriptingPlanNotificationEvent
    {
        public static readonly EventType<ScriptingPlanNotificationParams> Type = EventType<ScriptingPlanNotificationParams>.Create("scripting/scriptPlanNotification");
    }
}
