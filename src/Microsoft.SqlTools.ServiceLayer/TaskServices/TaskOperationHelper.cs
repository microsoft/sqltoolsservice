//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// Helper class for task operations
    /// </summary>
    public static class TaskOperationHelper
    {
        /// <summary>
        /// Async method to execute the operation
        /// </summary>
        /// <param name="sqlTask">Sql Task</param>
        /// <returns>Task Result</returns>
        public static async Task<TaskResult> ExecuteTaskAsync(SqlTask sqlTask)
        {
            sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
            ITaskOperation taskOperation = sqlTask.TaskMetadata.TaskOperation as ITaskOperation;
            TaskResult taskResult = null;

            if (taskOperation != null)
            {
                taskOperation.SqlTask = sqlTask;
                
                return await Task.Factory.StartNew(() =>
                {
                    TaskResult result = new TaskResult();
                    try
                    {
                        if (string.IsNullOrEmpty(taskOperation.ErrorMessage))
                        {
                            taskOperation.Execute(sqlTask.TaskMetadata.TaskExecutionMode);
                            result.TaskStatus = SqlTaskStatus.Succeeded;
                        }
                        else
                        {
                            result.TaskStatus = SqlTaskStatus.Failed;
                            result.ErrorMessage = taskOperation.ErrorMessage;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.TaskStatus = SqlTaskStatus.Failed;
                        result.ErrorMessage = ex.Message;
                        if (ex.InnerException != null)
                        {
                            result.ErrorMessage += Environment.NewLine + ex.InnerException.Message;
                        }
                        if (taskOperation != null && taskOperation.ErrorMessage != null)
                        {
                            result.ErrorMessage += Environment.NewLine + taskOperation.ErrorMessage;
                        }
                    }
                    return result;
                });
            }
            else
            {
                taskResult = new TaskResult();
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return taskResult;
        }

        /// <summary>
        /// Async method to cancel the operations
        /// </summary>
        public static async Task<TaskResult> CancelTaskAsync(SqlTask sqlTask)
        {
            ITaskOperation taskOperation = sqlTask.TaskMetadata.TaskOperation as ITaskOperation;
            TaskResult taskResult = null;


            if (taskOperation != null)
            {
              
                return await Task.Factory.StartNew(() =>
                {
                    try
                    {
                        taskOperation.Cancel();

                        return new TaskResult
                        {
                            TaskStatus = SqlTaskStatus.Canceled
                        };
                    }
                    catch (Exception ex)
                    {
                        return new TaskResult
                        {
                            TaskStatus = SqlTaskStatus.Failed,
                            ErrorMessage = ex.Message
                        };
                    }
                });
            }
            else
            {
                taskResult = new TaskResult();
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return taskResult;
        }
    }
}
