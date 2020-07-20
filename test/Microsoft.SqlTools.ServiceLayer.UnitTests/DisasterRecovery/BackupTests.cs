//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    public class BackupTests
    {
        /// <summary>
        /// Create and run a backup task
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyRunningBackupTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                var mockBackupOperation = new Mock<IBackupOperation>();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(mockBackupOperation.Object);
                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);
                Assert.NotNull(sqlTask);
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                await taskToVerify;
            }
        }

        /// <summary>
        /// Generate script for backup task
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyScriptBackupTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                var mockBackupOperation = new Mock<IBackupOperation>();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(mockBackupOperation.Object);
                taskMetaData.TaskExecutionMode = TaskExecutionMode.Script;

                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);
                Assert.NotNull(sqlTask);
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                await taskToVerify;
            }
        }

        /// <summary>
        /// Create and run multiple backup tasks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyRunningMultipleBackupTasks()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                var mockBackupOperation = new Mock<IBackupOperation>();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(mockBackupOperation.Object);

                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);
                SqlTask sqlTask2 = manager.CreateTask<SqlTask>(taskMetaData);
                Assert.NotNull(sqlTask);
                Assert.NotNull(sqlTask2);

                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                Task taskToVerify2 = sqlTask2.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask2.TaskStatus);
                });

                await Task.WhenAll(taskToVerify, taskToVerify2);
            }
        }

        /// <summary>
        /// Cancel a backup task
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyCancelBackupTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                IBackupOperation backupOperation = new BackupOperationStub();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(backupOperation);
                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);
                Assert.NotNull(sqlTask);
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Canceled, sqlTask.TaskStatus);
                    Assert.Equal(true, sqlTask.IsCancelRequested);
                    ((BackupOperationStub)backupOperation).BackupSemaphore.Release();
                    manager.Reset();
                });

                manager.CancelTask(sqlTask.TaskId);
                await taskToVerify;
            }
        }

        /// <summary>
        /// Cancel multiple backup tasks
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyCancelMultipleBackupTasks()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                IBackupOperation backupOperation = new BackupOperationStub();
                IBackupOperation backupOperation2 = new BackupOperationStub();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(backupOperation);
                TaskMetadata taskMetaData2 = this.CreateTaskMetaData(backupOperation2);

                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);
                SqlTask sqlTask2 = manager.CreateTask<SqlTask>(taskMetaData2);
                Assert.NotNull(sqlTask);
                Assert.NotNull(sqlTask2);

                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Canceled, sqlTask.TaskStatus);
                    Assert.Equal(true, sqlTask.IsCancelRequested);
                    ((BackupOperationStub)backupOperation).BackupSemaphore.Release();
                    manager.Reset();
                });

                Task taskToVerify2 = sqlTask2.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Canceled, sqlTask2.TaskStatus);
                    Assert.Equal(true, sqlTask2.IsCancelRequested);
                    ((BackupOperationStub)backupOperation2).BackupSemaphore.Release();
                    manager.Reset();
                });

                manager.CancelTask(sqlTask.TaskId);
                manager.CancelTask(sqlTask2.TaskId);
                await Task.WhenAll(taskToVerify, taskToVerify2);
            }
        }

        /// <summary>
        /// Create two backup tasks and cancel one task
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task VerifyCombinationRunAndCancelBackupTasks()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                IBackupOperation backupOperation = new BackupOperationStub();
                TaskMetadata taskMetaData = this.CreateTaskMetaData(backupOperation);
                SqlTask sqlTask = manager.CreateTask<SqlTask>(taskMetaData);

                var mockBackupOperation = new Mock<IBackupOperation>();
                TaskMetadata taskMetaData2 = this.CreateTaskMetaData(mockBackupOperation.Object);
                SqlTask sqlTask2 = manager.CreateTask<SqlTask>(taskMetaData);

                Assert.NotNull(sqlTask);
                Assert.NotNull(sqlTask2);

                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Canceled, sqlTask.TaskStatus);
                    Assert.Equal(true, sqlTask.IsCancelRequested);
                    ((BackupOperationStub)backupOperation).BackupSemaphore.Release();
                    manager.Reset();
                });

                Task taskToVerify2 = sqlTask2.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask2.TaskStatus);
                });

                manager.CancelTask(sqlTask.TaskId);
                await Task.WhenAll(taskToVerify, taskToVerify2); 
            }
        }

        private TaskMetadata CreateTaskMetaData(IBackupOperation data)
        {
            TaskMetadata taskMetaData = new TaskMetadata
            {
                ServerName = "server name",
                DatabaseName = "database name",
                Name = "backup database",
                TaskOperation = data
            };

            return taskMetaData;
        }
    }
}
