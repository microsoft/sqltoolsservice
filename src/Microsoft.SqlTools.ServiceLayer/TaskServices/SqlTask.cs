﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using Microsoft.SqlTools.Utility;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// A wrapper to a long running database operation. The class holds a refrence to the actual task that's running 
    /// and keeps track of the task status to send notifications
    /// </summary>
    public class SqlTask : IDisposable
    {
        private bool isCompleted;
        private bool isCancelRequested;
        private bool isDisposed;
        private readonly object lockObject = new object();
        private readonly List<TaskMessage> messages = new List<TaskMessage>();

        private DateTime startTime;
        private SqlTaskStatus status = SqlTaskStatus.NotStarted;
        private DateTime stopTime;

        public event EventHandler<TaskEventArgs<TaskScript>> ScriptAdded;
        public event EventHandler<TaskEventArgs<TaskMessage>> MessageAdded;
        public event EventHandler<TaskEventArgs<SqlTaskStatus>> StatusChanged;

        /// <summary>
        /// Default constructor to create the geenric type. calling Initialize method is required after creating
        /// the insance
        /// </summary>
        public SqlTask()
        {
        }

        /// <summary>
        /// Creates new instance of SQL task
        /// </summary>
        /// <param name="taskMetdata">Task Metadata</param>
        /// <param name="taskToRun">The function to run to start the task</param>
        public SqlTask(TaskMetadata taskMetdata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel)
        {
            Init(taskMetdata, taskToRun, taskToCancel);
        }

        /// <summary>
        /// Initializes the Sql task
        /// </summary>
        /// <param name="taskMetadata">Task metadata</param>
        /// <param name="taskToRun">The function to execute the operation</param>
        /// <param name="taskToCancel">The function to cancel the operation</param>
        public virtual void Init(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel)
        {
            Validate.IsNotNull(nameof(taskMetadata), taskMetadata);
            Validate.IsNotNull(nameof(taskToRun), taskToRun);

            TaskMetadata = taskMetadata;
            TaskToRun = taskToRun;
            TaskToCancel = taskToCancel;
            StartTime = DateTime.UtcNow;
            TaskId = Guid.NewGuid();
            TokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancellation token
        /// </summary>
        public CancellationTokenSource TokenSource { get; private set; }

        /// <summary>
        /// Task Metadata
        /// </summary>
        internal TaskMetadata TaskMetadata { get; private set; }

        /// <summary>
        /// The function to run 
        /// </summary>
        private Func<SqlTask, Task<TaskResult>> TaskToRun
        {
            get;
            set;
        }

        /// <summary>
        /// The function to cancel the operation 
        /// </summary>
        private Func<SqlTask, Task<TaskResult>> TaskToCancel
        {
            get;
            set;
        }

        /// <summary>
        /// Task unique id
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// Starts the task and monitor the task progress
        /// </summary>
        public async Task RunAsync()
        {
            TaskStatus = SqlTaskStatus.InProgress;
            await RunAndCancel().ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                {
                    TaskResult taskResult = task.Result;
                    TaskStatus = taskResult.TaskStatus;
                }
                else if (task.IsCanceled)
                {
                    TaskStatus = SqlTaskStatus.Canceled;
                }
                else if (task.IsFaulted)
                {
                    TaskStatus = SqlTaskStatus.Failed;
                    if (task.Exception != null)
                    {
                        AddMessage(task.Exception.Message);
                    }
                }
            });
        }

        /// <summary>
        /// Create a backup task for execution and cancellation
        /// </summary>
        /// <param name="sqlTask"></param>
        /// <returns></returns>
        internal async Task<TaskResult> RunAndCancel()
        {
            TokenSource = new CancellationTokenSource();
            AddMessage(SR.TaskInProgress, SqlTaskStatus.InProgress, true);
           
            TaskResult taskResult = new TaskResult();
            Task<TaskResult> performTask = TaskToRun(this);
            Task<TaskResult> completedTask = null;

            try
            {
                using (AutoResetEvent onCompletedEvent = new AutoResetEvent(initialState: false))
                {
                    if (TaskToCancel != null)
                    {
                        Task<TaskResult> cancelTask = Task.Run(() => CancelTaskAsync(TokenSource.Token, onCompletedEvent));

                        completedTask = await Task.WhenAny(performTask, cancelTask);

                        // Release the cancelTask
                        if (completedTask == performTask)
                        {
                            onCompletedEvent.Set();
                        }
                    }
                    else
                    {
                        completedTask = await Task.WhenAny(performTask);
                    }

                    AddMessage(completedTask.Result.TaskStatus == SqlTaskStatus.Failed ? completedTask.Result.ErrorMessage : SR.TaskCompleted,
                                       completedTask.Result.TaskStatus);
                    taskResult = completedTask.Result;
                }
            }
            catch (OperationCanceledException)
            {
                taskResult.TaskStatus = SqlTaskStatus.Canceled;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException is OperationCanceledException)
                {
                    taskResult.TaskStatus = SqlTaskStatus.Canceled;
                }
                else
                {
                    taskResult.TaskStatus = SqlTaskStatus.Failed;
                    AddMessage(ex.Message);
                }
            }
            return taskResult;
        }

        /// <summary>
        /// Async task to cancel backup
        /// </summary>
        /// <param name="backupOperation"></param>
        /// <param name="token"></param>
        /// <param name="onCompletedEvent"></param>
        /// <returns></returns>
        private async Task<TaskResult> CancelTaskAsync(CancellationToken token, AutoResetEvent onCompletedEvent)
        {
            // Create a task for backup cancellation request

            TaskResult result = new TaskResult();
            WaitHandle[] waitHandles = new WaitHandle[2]
            {
                    onCompletedEvent,
                    token.WaitHandle
            };

            WaitHandle.WaitAny(waitHandles);
            try
            {
                if (token.IsCancellationRequested)
                {
                    await this.TaskToCancel(this);
                    result.TaskStatus = SqlTaskStatus.Canceled;
                }
            }
            catch (Exception ex)
            {
                result.TaskStatus = SqlTaskStatus.Failed;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
        //Run Task synchronously 
        public void Run()
        {
            Task.Run(() => RunAsync());
        }

        /// <summary>
        /// Returns true if task has any messages
        /// </summary>
        public bool HasMessages
        {
            get
            {
                lock (lockObject)
                {
                    return messages.Any();
                }
            }
        }

        /// <summary>
        /// Setting this to True will not change the Slot status.
        /// Setting the Slot status to Canceled will set this to true.
        /// </summary>
        public bool IsCancelRequested
        {
            get
            {
                return isCancelRequested;
            }
            private set
            {
                if (isCancelRequested != value)
                {
                    isCancelRequested = value;
                    if (isCancelRequested)
                    {
                        TokenSource.Cancel();
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if task is canceled, failed or succeed 
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return isCompleted;
            }
            private set
            {
                if (isCompleted != value)
                {
                    isCompleted = value;
                    if (isCompleted)
                    {
                        StopTime = DateTime.UtcNow;
                    }
                }
            }
        }

        /// <summary>
        /// Task Messages
        /// </summary>
        internal ReadOnlyCollection<TaskMessage> Messages
        {
            get
            {
                lock (lockObject)
                {
                    return messages.AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Start Time
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return startTime;
            }
            internal set
            {
                startTime = value;
            }
        }

        /// <summary>
        /// The total number of seconds to run the task
        /// </summary>
        public double Duration
        {
            get
            {
                return (stopTime - startTime).TotalMilliseconds;
            }
        }
        

        /// <summary>
        /// Task Status
        /// </summary>
        public SqlTaskStatus TaskStatus
        {
            get
            {
                return status;
            }
            private set
            {
                status = value;
                switch (status)
                {
                    case SqlTaskStatus.Canceled:
                    case SqlTaskStatus.Failed:
                    case SqlTaskStatus.Succeeded:
                    case SqlTaskStatus.SucceededWithWarning:
                        IsCompleted = true;
                        break;
                    case SqlTaskStatus.InProgress:
                    case SqlTaskStatus.NotStarted:
                        IsCompleted = false;
                        break;
                    default:
                        throw new NotSupportedException("IsCompleted is not determined for status: " + status);
                }

                if (status == SqlTaskStatus.Canceled && !isCancelRequested)
                {
                    IsCancelRequested = true;
                }

                OnStatusChanged();
            }
        }

        /// <summary>
        /// The date time that the task was complete
        /// </summary>
        public DateTime StopTime
        {
            get
            {
                return stopTime;
            }
            internal set
            {
                stopTime = value;
            }
        }

        /// <summary>
        /// Try to cancel the task, and event to cancel the task will be raised 
        /// but the status won't change until that task actually get canceled by it's owner
        /// </summary>
        public void Cancel()
        {
            IsCancelRequested = true;
        }

        /// <summary>
        /// Add script result of the operation
        /// </summary>
        /// <param name="script">Script content</param>
        /// <param name="errorMessage">Error occured during script</param>
        /// <param name="status">Status of the script</param>
        /// <returns></returns>
        public TaskScript AddScript(SqlTaskStatus status, string script, string errorMessage = null)
        {
            var newScript = new TaskScript
            {
                Status = status,
                Script = script,
                ErrorMessage = errorMessage
            };

            OnScriptAdded(new TaskEventArgs<TaskScript>(newScript, this));
            return newScript;
        }

        /// <summary>
        /// Adds a new message to the task messages
        /// </summary>
        /// <param name="description">Message description</param>
        /// <param name="status">Status of the message</param>
        /// <param name="insertAboveLast">If true, the new messages will be added to the top. Default is false</param>
        /// <returns></returns>
        public TaskMessage AddMessage(string description, SqlTaskStatus status = SqlTaskStatus.NotStarted, bool insertAboveLast = false)
        {
            ValidateNotDisposed();

            if (!insertAboveLast)
            {
                // Make sure the last message is set to a completed status if a new message is being added at the bottom
                CompleteLastMessageStatus();
            }

            var newMessage = new TaskMessage
            {
                Description = description,
                Status = status,
            };

            lock (lockObject)
            {
                if (!insertAboveLast || messages.Count == 0)
                {
                    messages.Add(newMessage);
                }
                else
                {
                    int lastMessageIndex = messages.Count - 1;
                    messages.Insert(lastMessageIndex, newMessage);
                }
            }
            OnMessageAdded(new TaskEventArgs<TaskMessage>(newMessage, this));

            // If the slot is completed, this may be the last message, make sure the message is also set to completed.
            if (IsCompleted)
            {
                CompleteLastMessageStatus();
            }

            return newMessage;
        }

        /// <summary>
        /// Converts the task to Task info to be used in the contracts 
        /// </summary>
        /// <returns></returns>
        public TaskInfo ToTaskInfo()
        {
            return new TaskInfo
            {
                TaskId = this.TaskId.ToString(),
                DatabaseName = TaskMetadata.DatabaseName,
                ServerName = TaskMetadata.ServerName,
                Name = TaskMetadata.Name,
                Description = TaskMetadata.Description,
                TaskExecutionMode = TaskMetadata.TaskExecutionMode,
                IsCancelable = this.TaskToCancel != null,
                TargetLocation = TaskMetadata.TargetLocation,
            };
        }

        /// <summary>
        /// Makes sure the last message has a 'completed' status if it has a status of InProgress.
        /// If success is true, then sets the status to Succeeded.  Sets it to Failed if success is false.
        /// If success is null (default), then the message status is based on the status of the slot.
        /// </summary>
        private void CompleteLastMessageStatus(bool? success = null)
        {
            var message = GetLastMessage();
            if (message != null)
            {
                if (message.Status == SqlTaskStatus.InProgress)
                {
                    // infer the success boolean from the slot status if it's not set
                    if (success == null)
                    {
                        switch (TaskStatus)
                        {
                            case SqlTaskStatus.Canceled:
                            case SqlTaskStatus.Failed:
                                success = false;
                                break;
                            default:
                                success = true;
                                break;
                        }
                    }

                    message.Status = success.Value ? SqlTaskStatus.Succeeded : SqlTaskStatus.Failed;
                }
            }
        }

        private void OnScriptAdded(TaskEventArgs<TaskScript> e)
        {
            var handler = ScriptAdded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnMessageAdded(TaskEventArgs<TaskMessage> e)
        {
            var handler = MessageAdded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnStatusChanged()
        {
            var handler = StatusChanged;
            if (handler != null)
            {
                handler(this, new TaskEventArgs<SqlTaskStatus>(TaskStatus, this));
            }
        }

        public void Dispose()
        {
            //Dispose 
            isDisposed = true;
            TokenSource.Dispose();
        }

        protected void ValidateNotDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(typeof(SqlTask).FullName);
            }
        }

        /// <summary>
        /// Returns the most recently created message.  Returns null if there are no messages on the slot.
        /// </summary>
        public TaskMessage GetLastMessage()
        {
            ValidateNotDisposed();

            lock (lockObject)
            {
                if (messages.Count > 0)
                {
                    // get
                    return messages.Last();
                }
            }

            return null;
        }
    }
}
