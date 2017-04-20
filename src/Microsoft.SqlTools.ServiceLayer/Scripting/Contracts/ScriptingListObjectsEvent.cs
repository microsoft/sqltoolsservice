//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters sent when a list objects operation has completed.
    /// </summary>
    public class ScriptingListObjectsCompleteParameters : ScriptingCompleteParams
    {
        /// <summary>
        /// Gets or sets the list of database objects returned from the list objects operation.
        /// </summary>
        public List<ScriptingObject> DatabaseObjects { get; set; }

        /// <summary>
        /// Gets or sets the count of database object returned from the list objects operation.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Event sent to indicate a list objects operation has completed.
    /// </summary>
    public class ScriptingListObjectsCompleteEvent
    {
        public static readonly EventType<ScriptingListObjectsCompleteParameters> Type = EventType<ScriptingListObjectsCompleteParameters>.Create("scripting/listObjectsComplete");
    }
}
