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
        private TaskMetadata taskMetaData = new TaskMetadata
        {
            ServerName = "server name",
            DatabaseName = "database name",
            Name = "Backup Database", 
            IsCancelable = true
        };

        private TaskMetadata taskMetaData2 = new TaskMetadata
        {
            ServerName = "server name2",
            DatabaseName = "database name2",
            Name = "Backup Database2",
            IsCancelable = true
        };

        [Fact]
        public async Task VerifyCreateAndRunningBackupTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                var mockUtility = new Mock<IBackupUtilities>();
                DisasterRecoveryService service = new DisasterRecoveryService(mockUtility.Object);
                SqlTask sqlTask = manager.CreateTask(this.taskMetaData, service.BackupTask);
                Assert.NotNull(sqlTask);
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                await taskToVerify;
            }
        }

        [Fact]
        public async Task VerifyCreateAndRunningMultipleBackupTasks()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                var mockUtility = new Mock<IBackupUtilities>();
                DisasterRecoveryService service = new DisasterRecoveryService(mockUtility.Object);
                SqlTask sqlTask = manager.CreateTask(this.taskMetaData, service.BackupTask);
                SqlTask sqlTask2 = manager.CreateTask(this.taskMetaData2, service.BackupTask);
                Assert.NotNull(sqlTask);
                Assert.NotNull(sqlTask2);

                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                Task taskToVerify2 = sqlTask2.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Succeeded, sqlTask.TaskStatus);
                });

                await Task.WhenAll(taskToVerify, taskToVerify2);
            }
        }

        [Fact]
        public async Task VerifyCancelBackupTask()
        {
            using (SqlTaskManager manager = new SqlTaskManager())
            {
                IBackupUtilities backupUtility = new BackupUtilitiesStub();                
                DisasterRecoveryService service = new DisasterRecoveryService(backupUtility);
                SqlTask sqlTask = manager.CreateTask(this.taskMetaData, service.BackupTask);
                Assert.NotNull(sqlTask);
                Task taskToVerify = sqlTask.RunAsync().ContinueWith(Task =>
                {
                    Assert.Equal(SqlTaskStatus.Canceled, sqlTask.TaskStatus);
                    Assert.Equal(sqlTask.IsCancelRequested, true);
                    manager.Reset();
                });

                manager.CancelTask(sqlTask.TaskId);
                await taskToVerify;
            }
        }
    }
}
