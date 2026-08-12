//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
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
        public static Task<TaskResult> ExecuteTaskAsync(SqlTask sqlTask)
        {
            sqlTask.AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
            ITaskOperation taskOperation = sqlTask.TaskMetadata.TaskOperation as ITaskOperation;
            TaskResult taskResult = null;

            if (taskOperation != null)
            {
                taskOperation.SqlTask = sqlTask;

                return Task.Run(() =>
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
                        result.ErrorMessage = GetErrorMessage(ex, taskOperation.ErrorMessage);
                    }
                    return result;
                });
            }
            else
            {
                taskResult = new TaskResult();
                taskResult.TaskStatus = SqlTaskStatus.Failed;
            }

            return Task.FromResult(taskResult);
        }

        internal static string GetInnermostExceptionMessage(Exception exception)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception.Message;
        }

        private static string GetErrorMessage(Exception exception, string operationErrorMessage)
        {
            var messages = new List<string>();
            if (!string.IsNullOrWhiteSpace(operationErrorMessage))
            {
                messages.Add(operationErrorMessage);
            }

            string innermostExceptionMessage = GetInnermostExceptionMessage(exception);
            if (!string.IsNullOrWhiteSpace(innermostExceptionMessage) &&
                !messages.Contains(innermostExceptionMessage))
            {
                messages.Add(innermostExceptionMessage);
            }

            return string.Join(Environment.NewLine, messages);
        }

        /// <summary>
        /// Async method to cancel the operations
        /// </summary>
        public static Task<TaskResult> CancelTaskAsync(SqlTask sqlTask)
        {
            ITaskOperation taskOperation = sqlTask.TaskMetadata.TaskOperation as ITaskOperation;
            TaskResult taskResult = null;

            if (taskOperation != null)
            {

                return Task.Run(() =>
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

            return Task.FromResult(taskResult);
        }
    }
}
