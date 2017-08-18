//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// Task execution mode
    /// </summary>
    public enum TaskExecutionMode
    {
        /// <summary>
        /// Execute task
        /// </summary>
        Execute,

        /// <summary>
        /// Script task
        /// </summary>
        Script,

        /// <summary>
        /// Execute and script task
        /// Needed for tasks that will show the script when execution completes
        /// </summary>
        ExecuteAndScript
    }
}
