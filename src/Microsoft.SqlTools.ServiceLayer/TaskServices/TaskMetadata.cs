//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskMetadata
    {
        /// <summary>
        /// Task Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Task name to define the type of the task e.g. Create Db, back up
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Task operation type (e.g. execute or script)
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// The number of seconds to wait before canceling the task. 
        /// This is a optional field and 0 or negative numbers means no timeout
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Defines if the task can be canceled
        /// </summary>
        public bool IsCancelable { get; set; }

        /// <summary>
        /// Database server name this task is created for
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Database name this task is created for
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Data required to perform the task
        /// </summary>
        public object Data { get; set; }                
    }
}
