﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{

    public class TaskManagerTests
    {
        private TaskMetadata taskMetaData = new TaskMetadata
        {
            ServerName = "server name",
            DatabaseName = "database name"
        };

        [Test]
        public void ManagerInstanceWithNoTaskShouldNotBreakOnCancelTask()
        {
            SqlTaskManager manager = new SqlTaskManager();
            Assert.True(manager.Tasks.Count == 0);
            manager.CancelTask(Guid.NewGuid());
        }

        [Test]
        public async Task VerifyCreateAndRunningTask()
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
                    TaskStatus = SqlTaskStatus.Succeeded
                };
                SqlTask sqlTask = manager.CreateTask(taskMetaData, operation.FunctionToRun);
                Assert.NotNull(sqlTask);
                Assert.True(taskAddedEventRaised);

                Assert.False(manager.HasCompletedTasks());
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
                {
                    Assert.True(manager.HasCompletedTasks());
                    manager.RemoveCompletedTask(sqlTask);


                });
                operation.Stop();
                await taskToVerify;
            }

        }

        [Test]
        public async Task CancelTaskShouldCancelTheOperation()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                SqlTaskStatus expectedStatus = SqlTaskStatus.Canceled;

                DatabaseOperationStub operation = new DatabaseOperationStub();
                operation.TaskResult = new TaskResult
                {
                };
                SqlTask sqlTask = manager.CreateTask(taskMetaData, operation.FunctionToRun, operation.FunctionToCancel);
                Assert.NotNull(sqlTask);

                Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
                {
                    Assert.AreEqual(expectedStatus, sqlTask.TaskStatus);
                    Assert.AreEqual(true, sqlTask.IsCancelRequested);
                    manager.Reset();

                });
                manager.CancelTask(sqlTask.TaskId);
                await taskToVerify;
            }

        }

        [Test]
        public async Task VerifyScriptTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                DatabaseOperationStub operation = new DatabaseOperationStub();
                operation.TaskResult = new TaskResult
                {
                    TaskStatus = SqlTaskStatus.Succeeded
                };
                SqlTask sqlTask = manager.CreateTask(taskMetaData, operation.FunctionToScript);

                bool scriptAddedEventRaised = false;
                string script = null;
                sqlTask.ScriptAdded += (object sender, TaskEventArgs<TaskScript> e) =>
                {
                    scriptAddedEventRaised = true;
                    script = e.TaskData.Script;
                };

                Assert.NotNull(sqlTask);
                
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
                {
                    Assert.True(scriptAddedEventRaised);
                    Assert.True(!string.IsNullOrEmpty(script));
                    Assert.True(manager.HasCompletedTasks());
                    manager.RemoveCompletedTask(sqlTask);
                });
                operation.Stop();
                await taskToVerify;
            }

        }
    }
}
