//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Parameters sent when a scripting operation has made progress.
    /// </summary>
    public class ScriptingProgressNotificationParams : ScriptingEventParams
    {
        /// <summary>
        /// Gets or sets the scripting object whose progress has changed.
        /// </summary>
        public ScriptingObject ScriptingObject { get; set; }

        /// <summary>
        /// Gets or sets the status of the scripting operation for the scripting object.
        /// </summary>
        /// <remarks>
        /// Values can be: 'Completed', 'Progress', and 'Error'.
        /// </remarks>
        public string Status { get; set; }

        /// <summary>
        /// Gets or count of completed scripting operations.
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Gets this total count of objects to script.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the error details if an error occurred scripting a database object.
        /// </summary>
        public string ErrorDetails { get; set; }

        /// <summary>
        /// Get or sets the error message for an error that occurred scripting a database object.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event to indicate the scripting operation has made progress.
    /// </summary>
    public class ScriptingProgressNotificationEvent
    {
        public static readonly EventType<ScriptingProgressNotificationParams> Type = 
            EventType<ScriptingProgressNotificationParams>.Create("scripting/scriptProgressNotification");
    }
}
