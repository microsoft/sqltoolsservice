//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{
    public class TaskProgressInfo
    {
        /// <summary>
        /// An id to unify the task
        /// </summary>
        public string TaskId { get; set;  }

        /// <summary>
        /// Task status
        /// </summary>
        public SqlTaskStatus Status { get;  set; }

        /// <summary>
        /// Task type
        /// </summary>
        public SqlTaskType TaskType { get; set; }

        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Script for the operation(generate script or execution)
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The number of millisecond the task was running
        /// </summary>
        public double Duration { get; set; }

    }
}
