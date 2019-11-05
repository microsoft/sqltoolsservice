//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using Microsoft.SqlTools.ServiceLayer.Hosting;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts
{
    public class TaskInfo
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
        /// Task execution mode
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// Database server name this task is created for
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Database name this task is created for
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Task name which defines the type of the task (e.g. CreateDatabase, Backup)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Provider Name
        /// </summary>
        public string ProviderName
        {
            get
            {
                return ServiceHost.ProviderName;
            }
        }

        /// <summary>
        /// Task description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Defines if the task can be canceled
        /// </summary>
        public bool IsCancelable { get; set; }
    }
}
