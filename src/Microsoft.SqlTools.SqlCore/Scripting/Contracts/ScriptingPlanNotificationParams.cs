//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.SqlCore.Scripting.Contracts
{
    /// <summary>
    /// Parameters to indicate the script operation has resolved the objects to be scripted.
    /// </summary>
    public class ScriptingPlanNotificationParams : ScriptingEventParams
    {
        /// <summary>
        /// Gets or sets the list of database objects whose progress has changed.
        /// </summary>
        public List<ScriptingObject> ScriptingObjects { get; set; }

        /// <summary>
        /// Gets or sets the count of database objects whose progress has changed.
        /// </summary>
        public int Count { get; set; }
    }
}
