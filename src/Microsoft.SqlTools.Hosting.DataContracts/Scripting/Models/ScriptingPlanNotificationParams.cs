using System.Collections.Generic;

namespace Microsoft.SqlTools.Hosting.DataContracts.Scripting.Models
{
    /// <summary>
    /// Parameters to indicate the script operation has resolved the objects to be scripted.
    /// </summary>
    public class ScriptingPlanNotificationParams: ScriptingEventParams
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