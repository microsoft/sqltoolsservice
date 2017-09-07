//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Utility;

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
        /// Task execution mode (e.g. execute or script)
        /// </summary>
        public TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// The number of seconds to wait before canceling the task. 
        /// This is a optional field and 0 or negative numbers means no timeout
        /// </summary>
        public int Timeout { get; set; }

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
        public ITaskOperation TaskOperation { get; set; }

        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Creates task metadata given the request parameters
        /// </summary>
        /// <param name="requestParam">Request parameters</param>
        /// <param name="taskName">Task name</param>
        /// <param name="taskOperation">Task operation</param>
        /// <param name="connectionService">Connection Service</param>
        /// <returns>Task metadata</returns>
        public static TaskMetadata Create(IRequestParams requestParam, string taskName, ITaskOperation taskOperation, ConnectionService connectionService)
        {
            TaskMetadata taskMetadata = new TaskMetadata();
            ConnectionInfo connInfo;
            connectionService.TryFindConnection(
                    requestParam.OwnerUri,
                    out connInfo);

            if (connInfo != null)
            {
                taskMetadata.ServerName = connInfo.ConnectionDetails.ServerName;
            }

            if (connInfo != null)
            {
                taskMetadata.DatabaseName = connInfo.ConnectionDetails.DatabaseName;
            }

            IScriptableRequestParams scriptableRequestParams = requestParam as IScriptableRequestParams;
            if (scriptableRequestParams != null && scriptableRequestParams.TaskExecutionMode != TaskExecutionMode.Execute)
            {
                taskMetadata.Name = string.Format("{0} {1}", taskName, SR.ScriptTaskName);
            }
            else
            {
                taskMetadata.Name = taskName;
            }
            taskMetadata.TaskExecutionMode = scriptableRequestParams.TaskExecutionMode;

            taskMetadata.TaskOperation = taskOperation;
            taskMetadata.OwnerUri = requestParam.OwnerUri;

            return taskMetadata;
        }
       
    }
}
