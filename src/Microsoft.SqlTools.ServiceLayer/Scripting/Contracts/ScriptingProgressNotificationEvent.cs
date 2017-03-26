//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters to indicate the script operation has resolve the objects to be scripted.
    /// </summary>
    public class ScriptingProgressNotificationParams : ScriptingEventParams
    {
        public ScriptingObject ScriptingObject { get; set; }

        public string Status { get; set; }

        public int Count { get; set; }

        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Event to indicate the script operation has resolve the objects to be scripted.
    /// </summary>
    public class ScriptingProgressNotificationEvent
    {
        public static readonly EventType<ScriptingProgressNotificationParams> Type = 
            EventType<ScriptingProgressNotificationParams>.Create("scripting/scriptProgressNotification");
    }
}
