//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.TaskServices
{
    /// <summary>
    /// A singleton class to manager the current long running operations 
    /// </summary>
    public class SqlTaskManager : IDisposable
    {
        private static SqlTaskManager instance = new SqlTaskManager();
        private static readonly object lockObject = new object();
        private bool isDisposed;
        private readonly ConcurrentDictionary<Guid, SqlTask> tasks = new ConcurrentDictionary<Guid, SqlTask>();

        public event EventHandler<TaskEventArgs<SqlTask>> TaskAdded;
        public event EventHandler<TaskEventArgs<SqlTask>> TaskRemoved;


        /// <summary>
        /// Constructor to create an instance for test purposes use only
        /// </summary>
        internal SqlTaskManager()
        {

        }

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static SqlTaskManager Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Task connections
        /// </summary>
        internal ReadOnlyCollection<SqlTask> Tasks
        {
            get
            {
                lock (lockObject)
                {
                    return new ReadOnlyCollection<SqlTask>(tasks.Values.ToList());
                }
            }
        }

        /// <summary>
        /// Clear completed tasks
        /// </summary>
        internal void ClearCompletedTasks()
        {
            ValidateNotDisposed();

            lock (lockObject)
            {
                var tasksToRemove = (from task in tasks.Values
                                     where task.IsCompleted
                                     select task).ToList();
                foreach (var task in tasksToRemove)
                {
                    RemoveCompletedTask(task);
                }
            }
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <param name="taskToCancel">The function to cancel the operation</param>
        /// <returns>The new sql task</returns>
        public SqlTask CreateTask(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel) 
        {
            return CreateTask<SqlTask>(taskMetadata, taskToRun, taskToCancel);
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <returns>The new sql task</returns>
        public SqlTask CreateTask<T>(TaskMetadata taskMetadata) where T : SqlTask, new()
        {
            Validate.IsNotNull(nameof(taskMetadata), taskMetadata);
            return CreateTask<T>(taskMetadata, TaskOperationHelper.ExecuteTaskAsync, TaskOperationHelper.CancelTaskAsync);
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <param name="taskToCancel">The function to cancel the operation</param>
        /// <returns>The new sql task</returns>
        public SqlTask CreateTask<T>(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel) where T : SqlTask, new()
        {
            ValidateNotDisposed();

            var newTask = new T();
            newTask.Init(taskMetadata, taskToRun, taskToCancel);
            if (taskMetadata != null && taskMetadata.TaskOperation != null)
            {
                taskMetadata.TaskOperation.SqlTask = newTask;
            }

            lock (lockObject)
            {
                tasks.AddOrUpdate(newTask.TaskId, newTask, (key, oldValue) => newTask);
            }
            OnTaskAdded(new TaskEventArgs<SqlTask>(newTask));
            return newTask;
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <returns>The new sql task</returns>
        public SqlTask CreateTask(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun)
        {
            return CreateTask<SqlTask>(taskMetadata, taskToRun);
        }

        /// <summary>
        /// Creates a new task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <returns></returns>
        public SqlTask CreateTask<T>(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun) where T : SqlTask, new()
        {
            return CreateTask<T>(taskMetadata, taskToRun, null);
        }

        /// <summary>
        /// Creates a new task and starts the task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <param name="taskToCancel">The function to cancel the operation</param>
        /// <returns></returns>
        public SqlTask CreateAndRun(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel)
        {
            return CreateAndRun<SqlTask>(taskMetadata, taskToRun, taskToCancel);
        }

        public SqlTask CreateAndRun<T>(TaskMetadata taskMetadata) where T : SqlTask, new()
        {
            var sqlTask = CreateTask<T>(taskMetadata);
            sqlTask.Run();
            return sqlTask;
        }

        /// <summary>
        /// Creates a new task and starts the task
        /// </summary>
        /// <param name="taskMetadata">Task Metadata</param>
        /// <param name="taskToRun">The function to run the operation</param>
        /// <param name="taskToCancel">The function to cancel the operation</param>
        /// <returns></returns>
        public SqlTask CreateAndRun<T>(TaskMetadata taskMetadata, Func<SqlTask, Task<TaskResult>> taskToRun, Func<SqlTask, Task<TaskResult>> taskToCancel) where T : SqlTask, new()
        {
            var sqlTask = CreateTask<T>(taskMetadata, taskToRun, taskToCancel);
            sqlTask.Run();
            return sqlTask;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                lock (lockObject)
                {
                    foreach (var task in tasks.Values)
                    {
                        task.Dispose();
                    }
                    tasks.Clear();
                }
            }

            isDisposed = true;
        }

       /// <summary>
       /// Returns true if there's any completed task
       /// </summary>
       /// <returns></returns>
        internal bool HasCompletedTasks()
        {
            lock (lockObject)
            {
                return tasks.Values.Any(task => task.IsCompleted);
            }
        }

        private void OnTaskAdded(TaskEventArgs<SqlTask> e)
        {
            var handler = TaskAdded;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnTaskRemoved(TaskEventArgs<SqlTask> e)
        {
            var handler = TaskRemoved;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Cancel a task
        /// </summary>
        /// <param name="taskId"></param>
        public void CancelTask(Guid taskId)
        {
            SqlTask taskToCancel;

            lock (lockObject)
            {
                tasks.TryGetValue(taskId, out taskToCancel);
            }
            if (taskToCancel != null)
            {
                taskToCancel.Cancel();
            }
        }

        /// <summary>
        /// Internal for test purposes only.
        /// Removes all tasks regardless of status from the model.
        /// This is used as a test aid since Monitor is a singleton class.
        /// </summary>
        internal void Reset()
        {
            foreach (var task in tasks.Values)
            {
                RemoveTask(task);
            }
        }

        internal void RemoveCompletedTask(SqlTask task)
        {
            if (task.IsCompleted)
            {
                RemoveTask(task);
            }
        }

        private void RemoveTask(SqlTask task)
        {
            SqlTask removedTask;
            tasks.TryRemove(task.TaskId, out removedTask);
        }

        void ValidateNotDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(typeof(SqlTaskManager).FullName);
            }
        }
    }
}
