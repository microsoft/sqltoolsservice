//
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

        public event EventHandler<TaskEventArgs<TaskMessage>> MessageAdded;
        public event EventHandler<TaskEventArgs<SqlTaskStatus>> StatusChanged;
        public event EventHandler<TaskEventArgs<SqlTaskStatus>> TaskCanceled;

        /// <summary>
        /// Creates new instance of SQL task
        /// </summary>
        /// <param name="taskMetdata">Task Metadata</param>
        /// <param name="testToRun">The function to run to start the task</param>
        public SqlTask(TaskMetadata taskMetdata, Func<SqlTask, Task<TaskResult>> testToRun)
        {
            Validate.IsNotNull(nameof(taskMetdata), taskMetdata);
            Validate.IsNotNull(nameof(testToRun), testToRun);

            TaskMetadata = taskMetdata;
            TaskToRun = testToRun;
            StartTime = DateTime.UtcNow;
            TaskId = Guid.NewGuid();
        }

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
        /// Task unique id
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// Starts the task and monitor the task progress
        /// </summary>
        public async Task RunAsync()
        {
            TaskStatus = SqlTaskStatus.InProgress;
            await TaskToRun(this).ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsCanceled && !task.IsFaulted)
                {
                    TaskResult taskResult = task.Result;
                    TaskStatus = taskResult.TaskStatus;
                }
                else if(task.IsCanceled)
                {
                    TaskStatus = SqlTaskStatus.Canceled;
                }
                else if(task.IsFaulted)
                {
                    TaskStatus = SqlTaskStatus.Failed;
                    if(task.Exception != null)
                    {
                        AddMessage(task.Exception.Message);
                    }
                }
            });
        }

        //Run Task synchronously 
        public void Run()
        {
            RunAsync().ContinueWith(task =>
            {
            });
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
                    OnTaskCancelRequested();
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
        /// Try to cancel the task, and even to cancel the task will be raised 
        /// but the status won't change until that task actually get canceled by it's owner
        /// </summary>
        public void Cancel()
        {
            IsCancelRequested = true;
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

        private void OnTaskCancelRequested()
        {
            var handler = TaskCanceled;
            if (handler != null)
            {
                handler(this, new TaskEventArgs<SqlTaskStatus>(TaskStatus, this));
            }
        }

        public void Dispose()
        {
            //Dispose 
            isDisposed = true;
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
