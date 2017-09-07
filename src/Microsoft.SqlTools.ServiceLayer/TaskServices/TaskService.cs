//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Utility;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    public class TaskService: HostedService<TaskService>, IComposableService
    {
        private static readonly Lazy<TaskService> instance = new Lazy<TaskService>(() => new TaskService());
        private SqlTaskManager taskManager = null;
        private IProtocolEndpoint serviceHost;

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static TaskService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Task Manager Instance to use for testing
        /// </summary>
        internal SqlTaskManager TaskManager
        {
            get
            {
                if(taskManager == null)
                {
                    taskManager = SqlTaskManager.Instance;
                }
                return taskManager;
            }
            set
            {
                taskManager = value;
            }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public override void InitializeService(IProtocolEndpoint serviceHost)
        {
            this.serviceHost = serviceHost;
            Logger.Write(LogLevel.Verbose, "TaskService initialized");
            serviceHost.SetRequestHandler(ListTasksRequest.Type, HandleListTasksRequest);
            serviceHost.SetRequestHandler(CancelTaskRequest.Type, HandleCancelTaskRequest);
            TaskManager.TaskAdded += OnTaskAdded;
        }

        /// <summary>
        /// Handles a list tasks request
        /// </summary>
        internal async Task HandleListTasksRequest(
            ListTasksParams listTasksParams,
            RequestContext<ListTasksResponse> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleListTasksRequest");

            Func<Task<ListTasksResponse>> getAllTasks = () =>
            {
                Validate.IsNotNull(nameof(listTasksParams), listTasksParams);
                return Task.Factory.StartNew(() =>
                {
                    ListTasksResponse response = new ListTasksResponse();
                    response.Tasks = TaskManager.Tasks.Select(x => x.ToTaskInfo()).ToArray();

                    return response;
                });

            };

            await HandleRequestAsync(getAllTasks, context, "HandleListTasksRequest");
        }

        internal async Task HandleCancelTaskRequest(CancelTaskParams cancelTaskParams, RequestContext<bool> context)
        {
            Logger.Write(LogLevel.Verbose, "HandleCancelTaskRequest");
            Func<Task<bool>> cancelTask = () =>
            {
                Validate.IsNotNull(nameof(cancelTaskParams), cancelTaskParams);

                return Task.Factory.StartNew(() =>
                {
                    Guid taskId;
                    if (Guid.TryParse(cancelTaskParams.TaskId, out taskId))
                    {
                        TaskManager.CancelTask(taskId);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

            };

            await HandleRequestAsync(cancelTask, context, "HandleCancelTaskRequest");
        }

        private async void OnTaskAdded(object sender, TaskEventArgs<SqlTask> e)
        {
            SqlTask sqlTask = e.TaskData;
            if (sqlTask != null)
            {
                TaskInfo taskInfo = sqlTask.ToTaskInfo();
                sqlTask.ScriptAdded += OnTaskScriptAdded;
                sqlTask.MessageAdded += OnTaskMessageAdded;
                sqlTask.StatusChanged += OnTaskStatusChanged;
                await serviceHost.SendEvent(TaskCreatedNotification.Type, taskInfo);
            }
        }

        private async void OnTaskStatusChanged(object sender, TaskEventArgs<SqlTaskStatus> e)
        {
            SqlTask sqlTask = e.SqlTask;
            if (sqlTask != null)
            {
                TaskProgressInfo progressInfo = new TaskProgressInfo
                {
                    TaskId = sqlTask.TaskId.ToString(),
                    Status = e.TaskData
                };

                if (sqlTask.IsCompleted)
                {
                    progressInfo.Duration = sqlTask.Duration;
                }
                await serviceHost.SendEvent(TaskStatusChangedNotification.Type, progressInfo);
            }
        }
        
        private async void OnTaskScriptAdded(object sender, TaskEventArgs<TaskScript> e)
        {
            SqlTask sqlTask = e.SqlTask;
            if (sqlTask != null)
            {
                TaskProgressInfo progressInfo = new TaskProgressInfo
                {
                    TaskId = sqlTask.TaskId.ToString(),
                    Status = e.TaskData.Status,
                    Script = e.TaskData.Script,
                    Message = e.TaskData.ErrorMessage,
                };

                await serviceHost.SendEvent(TaskStatusChangedNotification.Type, progressInfo);
            }
        }

        private async void OnTaskMessageAdded(object sender, TaskEventArgs<TaskMessage> e)
        {
            SqlTask sqlTask = e.SqlTask;
            if (sqlTask != null)
            {
                TaskProgressInfo progressInfo = new TaskProgressInfo
                {
                    TaskId = sqlTask.TaskId.ToString(),
                    Message = e.TaskData.Description,
                    Status = sqlTask.TaskStatus
                };
                await serviceHost.SendEvent(TaskStatusChangedNotification.Type, progressInfo);
            }
        }

        public void Dispose()
        {
            TaskManager.TaskAdded -= OnTaskAdded;
            TaskManager.Dispose();
        }
    }
}
