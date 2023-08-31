//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.Scripting.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Event to indicate the scripting operation has made progress.
    /// </summary>
    public class ScriptingProgressNotificationEvent
    {
        public static readonly EventType<ScriptingProgressNotificationParams> Type = 
            EventType<ScriptingProgressNotificationParams>.Create("scripting/scriptProgressNotification");
    }
}
