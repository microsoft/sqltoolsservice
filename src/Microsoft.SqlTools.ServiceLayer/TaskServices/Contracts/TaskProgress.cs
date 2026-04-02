//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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
        /// Progress message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Script for the task execution
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The number of millisecond the task was running
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        /// Current progress value toward the goal
        /// </summary>
        public int ProgressCurrent { get; set; }

        /// <summary>
        /// Target progress value. 0 means indeterminate (heartbeat) progress.
        /// </summary>
        public int ProgressGoal { get; set; }

        /// <summary>
        /// Percentage of completion. -1 if indeterminate.
        /// </summary>
        public double PercentComplete { get; set; }

        /// <summary>
        /// Current phase or step name for multi-step operations
        /// </summary>
        public string Phase { get; set; }

    }
}
