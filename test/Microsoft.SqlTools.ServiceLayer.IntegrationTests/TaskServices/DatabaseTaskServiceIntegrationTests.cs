//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TaskServices
{
    public class DatabaseTaskServiceIntegrationTests
    {
        private readonly List<string> databasesToDrop = new List<string>();
        private const string OwnerUri = "DatabaseTaskServiceTests";

        [SetUp]
        public void SetUp()
        {
            AdminService.ConnectionServiceInstance = LiveConnectionHelper.GetLiveTestConnectionService();
            ObjectManagementService.ConnectionServiceInstance = LiveConnectionHelper.GetLiveTestConnectionService();
            SqlTaskManager.Instance.Reset();
        }

        [TearDown]
        public async Task TearDown()
        {
            foreach (string databaseName in databasesToDrop)
            {
                try
                {
                    await SqlTestDb.DropDatabase(databaseName);
                }
                catch
                {
                }
            }

            SqlTaskManager.Instance.Reset();
        }

        [Test]
        public async Task CreateDatabaseRequestCreatesCompletedTaskWithIndeterminateProgress()
        {
            string databaseName = GetUniqueDatabaseName("CreateTaskDb");
            databasesToDrop.Add(databaseName);

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", OwnerUri, Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default);
            var requestContext = new Mock<RequestContext<CreateDatabaseResponse>>();
            CreateDatabaseResponse response = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<CreateDatabaseResponse>()))
                .Callback<CreateDatabaseResponse>(r => response = r)
                .Returns(Task.FromResult(new object()));

            var databaseInfo = new Microsoft.SqlTools.ServiceLayer.Admin.Contracts.DatabaseInfo();
            databaseInfo.Options.Add("name", databaseName);

            var requestParams = new CreateDatabaseParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                DatabaseInfo = databaseInfo
            };

            await AdminService.HandleCreateDatabaseRequest(requestParams, requestContext.Object);

            Assert.That(response, Is.Not.Null);
            Assert.That(response.Result, Is.True);

            SqlTask sqlTask = await WaitForTaskAsync(task => task.TaskMetadata.OperationName == typeof(CreateDatabaseOperation).Name && task.TaskMetadata.DatabaseName == databaseName);

            Assert.That(sqlTask.TaskStatus, Is.EqualTo(SqlTaskStatus.Succeeded));
            Assert.That(sqlTask.TaskMetadata.Name, Is.EqualTo(global::Microsoft.SqlTools.ServiceLayer.SR.CreateDatabaseTaskName));
            Assert.That(sqlTask.PercentComplete, Is.EqualTo(-1));
            Assert.That(sqlTask.ProgressMessage, Is.EqualTo($"Create database '{databaseName}'."));
            Assert.That(sqlTask.Messages.Any(m => !string.IsNullOrWhiteSpace(m.Description)), Is.True);

            Assert.That(DatabaseExists(connectionResult.ConnectionInfo, databaseName), Is.True);
        }

        [Test]
        public async Task DropDatabaseRequestCreatesCompletedTaskWithIndeterminateProgress()
        {
            string databaseName = GetUniqueDatabaseName("DropTaskDb");
            await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, databaseName);
            databasesToDrop.Add(databaseName);

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", OwnerUri, Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default);
            var requestContext = new Mock<RequestContext<string>>();
            string response = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<string>()))
                .Callback<string>(r => response = r)
                .Returns(Task.FromResult(new object()));

            var requestParams = new DropDatabaseRequestParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                Database = databaseName,
                DropConnections = true,
                DeleteBackupHistory = false,
                GenerateScript = false,
            };

            await ObjectManagementTestUtils.Service.HandleDropDatabaseRequest(requestParams, requestContext.Object);

            Assert.That(response, Is.EqualTo(string.Empty));

            SqlTask sqlTask = await WaitForTaskAsync(task => task.TaskMetadata.OperationName == typeof(DropDatabaseOperation).Name && task.TaskMetadata.DatabaseName == databaseName);

            Assert.That(sqlTask.TaskStatus, Is.EqualTo(SqlTaskStatus.Succeeded));
            Assert.That(sqlTask.TaskMetadata.Name, Is.EqualTo(global::Microsoft.SqlTools.ServiceLayer.SR.DropDatabaseTaskName));
            Assert.That(sqlTask.PercentComplete, Is.EqualTo(-1));
            Assert.That(sqlTask.ProgressMessage, Is.EqualTo($"Drop database '{databaseName}'."));
            Assert.That(sqlTask.Messages.Any(m => !string.IsNullOrWhiteSpace(m.Description)), Is.True);

            Assert.That(DatabaseExists(connectionResult.ConnectionInfo, databaseName), Is.False);
            databasesToDrop.Remove(databaseName);
        }

        [Test]
        public async Task RenameDatabaseRequestCreatesCompletedTaskWithIndeterminateProgress()
        {
            string originalDatabaseName = GetUniqueDatabaseName("RenameTaskDb");
            string renamedDatabaseName = originalDatabaseName + "_renamed";
            await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, originalDatabaseName);
            databasesToDrop.Add(originalDatabaseName);
            databasesToDrop.Add(renamedDatabaseName);

            TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", OwnerUri, Microsoft.SqlTools.ServiceLayer.Connection.ConnectionType.Default);
            var requestContext = new Mock<RequestContext<RenameRequestResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<RenameRequestResponse>()))
                .Returns(Task.FromResult(new object()));

            var requestParams = new RenameRequestParams
            {
                ConnectionUri = connectionResult.ConnectionInfo.OwnerUri,
                ObjectType = SqlObjectType.Database,
                ObjectUrn = $"Server/Database[@Name='{originalDatabaseName}']",
                NewName = renamedDatabaseName,
            };

            await ObjectManagementTestUtils.Service.HandleRenameRequest(requestParams, requestContext.Object);

            SqlTask sqlTask = await WaitForTaskAsync(task => task.TaskMetadata.OperationName == "RenameObjectOperation" && task.TaskMetadata.DatabaseName == renamedDatabaseName);

            Assert.That(sqlTask.TaskStatus, Is.EqualTo(SqlTaskStatus.Succeeded));
            Assert.That(sqlTask.TaskMetadata.Name, Is.EqualTo(string.Format(CultureInfo.CurrentCulture, global::Microsoft.SqlTools.ServiceLayer.SR.RenameTaskName, originalDatabaseName)));
            Assert.That(sqlTask.PercentComplete, Is.EqualTo(-1));
            Assert.That(sqlTask.ProgressMessage, Is.EqualTo($"Rename database to '{renamedDatabaseName}'."));
            Assert.That(sqlTask.Messages.Any(m => !string.IsNullOrWhiteSpace(m.Description)), Is.True);

            Assert.That(DatabaseExists(connectionResult.ConnectionInfo, originalDatabaseName), Is.False);
            Assert.That(DatabaseExists(connectionResult.ConnectionInfo, renamedDatabaseName), Is.True);
            databasesToDrop.Remove(originalDatabaseName);
        }

        private static async Task<SqlTask> WaitForTaskAsync(Func<SqlTask, bool> predicate, int retries = 60, int delayMs = 500)
        {
            for (int attempt = 0; attempt < retries; attempt++)
            {
                SqlTask task = SqlTaskManager.Instance.Tasks.FirstOrDefault(predicate);
                if (task != null)
                {
                    for (int completionAttempt = 0; completionAttempt < retries && !task.IsCompleted; completionAttempt++)
                    {
                        await Task.Delay(delayMs);
                    }

                    return task;
                }

                await Task.Delay(delayMs);
            }

            Assert.Fail("Expected SQL task was not created.");
            return null;
        }

        private static bool DatabaseExists(ConnectionInfo connectionInfo, string databaseName)
        {
            ServerConnection serverConnection = ConnectionService.OpenServerConnection(connectionInfo, "DatabaseTaskServiceTests");
            try
            {
                using (serverConnection.SqlConnectionObject)
                {
                    Server server = new Server(serverConnection);
                    return server.Databases.Contains(databaseName);
                }
            }
            finally
            {
                if (serverConnection.SqlConnectionObject.State == System.Data.ConnectionState.Open)
                {
                    serverConnection.SqlConnectionObject.Close();
                }
            }
        }

        private static string GetUniqueDatabaseName(string prefix)
        {
            return $"{prefix}_{Guid.NewGuid():N}";
        }
    }
}