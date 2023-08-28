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
    /// Event sent to indicate a scripting operation has completed.
    /// </summary>
    public class ScriptingCompleteEvent
    {
        public static readonly EventType<ScriptingCompleteParams> Type = 
            EventType<ScriptingCompleteParams>.Create("scripting/scriptComplete");
    }
}
