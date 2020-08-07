//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{
    public class SqlTaskTests
    {
        [Test]
        public void CreateSqlTaskGivenInvalidArgumentShouldThrowException()
        {
            DatabaseOperationStub operation = new DatabaseOperationStub();

            Assert.Throws<ArgumentNullException>(() => new SqlTask(null, operation.FunctionToRun, operation.FunctionToCancel));
            Assert.Throws<ArgumentNullException>(() => new SqlTask(new TaskMetadata(), null, null));
        }

        [Test]
        public void CreateSqlTaskShouldGenerateANewId()
        {
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), new DatabaseOperationStub().FunctionToRun, null);
            Assert.NotNull(sqlTask.TaskId);
            Assert.True(sqlTask.TaskId != Guid.Empty);

            SqlTask sqlTask2 = new SqlTask(new TaskMetadata(), new DatabaseOperationStub().FunctionToRun, null);
            Assert.False(sqlTask.TaskId.CompareTo(sqlTask2.TaskId) == 0);
        }

        [Test]
        public async Task RunShouldRunTheFunctionAndGetTheResult()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Succeeded;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun, null);
            Assert.AreEqual(SqlTaskStatus.NotStarted, sqlTask.TaskStatus);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task => {
                Assert.AreEqual(sqlTask.TaskStatus, expectedStatus);
                Assert.AreEqual(true, sqlTask.IsCompleted);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.AreEqual(SqlTaskStatus.InProgress, sqlTask.TaskStatus);
            Thread.Sleep(1000);
            operation.Stop();
            await taskToVerify;
        }

        [Test]
        public async Task ToTaskInfoShouldReturnTaskInfo()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Succeeded;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata
            {
                ServerName = "server name",
                DatabaseName = "database name"
            }, operation.FunctionToRun, operation.FunctionToCancel);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
            {
                var taskInfo = sqlTask.ToTaskInfo();
                Assert.AreEqual(taskInfo.TaskId, sqlTask.TaskId.ToString());
                Assert.AreEqual("server name", taskInfo.ServerName);
                Assert.AreEqual("database name", taskInfo.DatabaseName);
            });
            operation.Stop();
            await taskToVerify;
        }

        [Test]
        public async Task FailedOperationShouldReturnTheFailedResult()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Failed;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun, operation.FunctionToCancel);
            Assert.AreEqual(SqlTaskStatus.NotStarted, sqlTask.TaskStatus);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task => {
                Assert.AreEqual(sqlTask.TaskStatus, expectedStatus);
                Assert.AreEqual(true, sqlTask.IsCompleted);
               // Assert.True(sqlTask.Duration > 0);
            });
            Assert.AreEqual(SqlTaskStatus.InProgress, sqlTask.TaskStatus);
            Thread.Sleep(1000);
            operation.Stop();
            await taskToVerify;
        }

        [Test]
        public async Task CancelingTheTaskShouldCancelTheOperation()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Canceled;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun, operation.FunctionToCancel);
            Assert.AreEqual(SqlTaskStatus.NotStarted, sqlTask.TaskStatus);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task => {
                Assert.AreEqual(sqlTask.TaskStatus, expectedStatus);
                Assert.AreEqual(true, sqlTask.IsCancelRequested);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.AreEqual(SqlTaskStatus.InProgress, sqlTask.TaskStatus);
            Thread.Sleep(1000);
            sqlTask.Cancel();
            await taskToVerify;
        }

        [Test]
        public async Task FailedOperationShouldFailTheTask()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Failed;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToRun, operation.FunctionToCancel);
            Assert.AreEqual(SqlTaskStatus.NotStarted, sqlTask.TaskStatus);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task => {
                Assert.AreEqual(sqlTask.TaskStatus, expectedStatus);
                Assert.True(sqlTask.Duration > 0);
            });
            Assert.AreEqual(SqlTaskStatus.InProgress, sqlTask.TaskStatus);
            Thread.Sleep(1000);
            operation.FailTheOperation();
            await taskToVerify;
        }

        [Test]
        public async Task RunScriptShouldReturnScriptContent()
        {
            SqlTaskStatus expectedStatus = SqlTaskStatus.Succeeded;
            DatabaseOperationStub operation = new DatabaseOperationStub();
            operation.TaskResult = new TaskResult
            {
                TaskStatus = expectedStatus
            };
            SqlTask sqlTask = new SqlTask(new TaskMetadata(), operation.FunctionToScript, null);
            Assert.AreEqual(SqlTaskStatus.NotStarted, sqlTask.TaskStatus);

            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task => {
                Assert.AreEqual(sqlTask.TaskStatus, expectedStatus);
                Assert.AreEqual(true, sqlTask.IsCompleted);
                Assert.NotNull(operation.TaskScript);
                Assert.True(!string.IsNullOrEmpty(operation.TaskScript.Script));
            });
            await taskToVerify;
        }
    }
}
