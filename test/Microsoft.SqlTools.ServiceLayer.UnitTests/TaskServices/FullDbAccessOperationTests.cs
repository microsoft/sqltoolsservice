//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{
    /// <summary>
    /// Operations that need full access to their database, such as restore, must settle their
    /// target database before taking access, so access is taken and released on the database
    /// they actually use.
    /// </summary>
    public class FullDbAccessOperationTests
    {
        [Test]
        public async Task AccessIsTakenOnTheDatabaseChosenByPreparation()
        {
            var events = new List<string>();
            using DatabaseLocksManager locksManager = CreateLocksManager(events);
            var operation = new RecordingOperation(events) { LockedDatabaseManager = locksManager };

            TaskResult result = await TaskOperationHelper.ExecuteTaskAsync(CreateSqlTask(operation));

            Assert.That(result.TaskStatus, Is.EqualTo(SqlTaskStatus.Succeeded));
            Assert.That(events, Is.EqualTo(new[] { "prepare", "close target", "execute target", "open target" }));
        }

        [Test]
        public async Task AccessIsReleasedWhenTheOperationFails()
        {
            var events = new List<string>();
            using DatabaseLocksManager locksManager = CreateLocksManager(events);
            var operation = new RecordingOperation(events)
            {
                LockedDatabaseManager = locksManager,
                ExecuteFailure = new InvalidOperationException("restore failed")
            };

            TaskResult result = await TaskOperationHelper.ExecuteTaskAsync(CreateSqlTask(operation));

            Assert.That(result.TaskStatus, Is.EqualTo(SqlTaskStatus.Failed));
            Assert.That(events, Is.EqualTo(new[] { "prepare", "close target", "execute target", "open target" }));
        }

        private static SqlTask CreateSqlTask(ITaskOperation operation)
        {
            return new SqlTask(
                new TaskMetadata { TaskOperation = operation, TaskExecutionMode = TaskExecutionMode.Execute },
                TaskOperationHelper.ExecuteTaskAsync,
                null);
        }

        private static DatabaseLocksManager CreateLocksManager(List<string> events)
        {
            var queue = new Mock<IConnectedBindingQueue>();
            queue.Setup(q => q.CloseConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback((string server, string database, int timeout) => events.Add($"close {database}"))
                .Returns(Task.CompletedTask);
            queue.Setup(q => q.OpenConnectionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Callback((string server, string database, int timeout) => events.Add($"open {database}"))
                .Returns(Task.CompletedTask);

            var connectionService = new ConnectionService();
            connectionService.RegisterConnectedQueue("test", queue.Object);
            return new DatabaseLocksManager(2000) { ConnectionService = connectionService };
        }

        /// <summary>
        /// Like restore, this operation only learns its target database while preparing.
        /// </summary>
        private sealed class RecordingOperation : SmoScriptableOperationWithFullDbAccess
        {
            private readonly List<string> events;
            private string databaseName = "stale";

            public RecordingOperation(List<string> events)
            {
                this.events = events;
            }

            public Exception ExecuteFailure { get; set; }

            public override string ErrorMessage => null;

            public override Server Server => null;

            public override string ServerName => "server";

            public override string DatabaseName => this.databaseName;

            public override void Cancel()
            {
            }

            public override void Execute()
            {
            }

            public override void Execute(TaskExecutionMode mode)
            {
                this.events.Add($"execute {this.databaseName}");
                if (this.ExecuteFailure != null)
                {
                    throw this.ExecuteFailure;
                }
            }

            protected override void PrepareToExecute()
            {
                this.events.Add("prepare");
                this.databaseName = "target";
            }
        }
    }
}
