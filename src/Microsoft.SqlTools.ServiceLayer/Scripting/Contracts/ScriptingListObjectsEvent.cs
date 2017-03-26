//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters to be sent to a indicate a list objects operation has completed.
    /// </summary>
    public class ScriptingListObjectsCompleteParameters : ScriptingEventParams
    {
        public List<ScriptingObject> DatabaseObjects { get; set; }

        public int Count { get; set; }
    }

    /// <summary>
    /// Event sent to a indicate a list objects operation has completed.
    /// </summary>
    public class ScriptingListObjectsCompleteEvent
    {
        public static readonly EventType<ScriptingListObjectsCompleteParameters> Type = EventType<ScriptingListObjectsCompleteParameters>.Create("scripting/listObjectsComplete");
    }
}
