//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{

    public class TaskManagerTests
    {
        private TaskMetadata taskMetaData = new TaskMetadata
        {
            ServerName = "server name",
            DatabaseName = "database name"
        };

        [Fact]
        public void ManagerInstanceWithNoTaskShouldNotBreakOnCancelTask()
        {
            SqlTaskManager manager = new SqlTaskManager();
            Assert.True(manager.Tasks.Count == 0);
            manager.CancelTask(Guid.NewGuid());
        }

        [Fact]
        public void VerifyCreateAndRunningTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                bool taskAddedEventRaised = false;
                manager.TaskAdded += (object sender, TaskEventArgs<SqlTask> e) =>
                {
                    taskAddedEventRaised = true;
                };
                DatabaseOperationStub operation = new DatabaseOperationStub();
                operation.TaskResult = new TaskResult
                {
                };
                SqlTask sqlTask = manager.CreateTask(taskMetaData, operation.FunctionToRun);
                Assert.NotNull(sqlTask);
                Assert.True(taskAddedEventRaised);

                Assert.False(manager.HasCompletedTasks());
                sqlTask.Run().ContinueWith(task =>
                {
                    Assert.True(manager.HasCompletedTasks());
                    manager.RemoveCompletedTask(sqlTask);


                });
                operation.Stop();
            }

        }

        [Fact]
        public void CancelTaskShouldCancelTheOperation()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                SqlTaskStatus expectedStatus = SqlTaskStatus.Canceled;

                DatabaseOperationStub operation = new DatabaseOperationStub();
                operation.TaskResult = new TaskResult
                {
                };
                SqlTask sqlTask = manager.CreateTask(taskMetaData, operation.FunctionToRun);
                Assert.NotNull(sqlTask);

                sqlTask.Run().ContinueWith(task =>
                {
                    Assert.Equal(sqlTask.TaskStatus, expectedStatus);
                    Assert.Equal(sqlTask.IsCancelRequested, true);
                    manager.Reset();

                });
                manager.CancelTask(sqlTask.TaskId);
            }

        }
    }
}
