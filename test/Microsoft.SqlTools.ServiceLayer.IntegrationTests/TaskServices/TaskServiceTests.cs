//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.UnitTests;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TaskServices
{
    public class TaskServiceTests : ServiceTestBase
    {
        private TaskService service;
        private Mock<IProtocolEndpoint> serviceHostMock;

        public TaskServiceTests()
        {
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateService();
            service.InitializeService(serviceHostMock.Object);
        }

        [Fact]
        public async Task VerifyTaskExecuteTheQueryGivenExecutionModeExecute()
        {
            await VerifyTaskWithExecutionMode(TaskExecutionMode.Execute);
        }

        [Fact]
        public async Task VerifyTaskGenerateScriptOnlyGivenExecutionModeScript()
        {
            await VerifyTaskWithExecutionMode(TaskExecutionMode.Script);
        }

        [Fact]
        public async Task VerifyTaskNotExecuteAndGenerateScriptGivenExecutionModeExecuteAndScript()
        {
            await VerifyTaskWithExecutionMode(TaskExecutionMode.ExecuteAndScript);
        }

        [Fact]
        public async Task VerifyTaskSendsFailureNotificationGivenInvalidQuery()
        {
            await VerifyTaskWithExecutionMode(TaskExecutionMode.ExecuteAndScript, true);
        }

        public async Task VerifyTaskWithExecutionMode(TaskExecutionMode executionMode, bool makeTaskFail = false)
        {
            serviceHostMock.AddEventHandling(TaskStatusChangedNotification.Type, null);

            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                //To make the task fail don't create the schema so create table fails
                string query = string.Empty;
                if (!makeTaskFail)
                {
                    query = $"CREATE SCHEMA [test]";
                }
                SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, "TaskService");
                try
                {
                    TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath);
                    string taskName = "task name";
                    Server server = CreateServerObject(connectionResult.ConnectionInfo);
                    RequstParamStub requstParam = new RequstParamStub
                    {
                        TaskExecutionMode = executionMode,
                        OwnerUri = queryTempFile.FilePath
                    };
                    SmoScriptableTaskOperationStub taskOperation = new SmoScriptableTaskOperationStub(server);
                    taskOperation.DatabaseName = testDb.DatabaseName;
                    taskOperation.TableName = "newTable";
                    TaskMetadata taskMetadata = TaskMetadata.Create(requstParam, taskName, taskOperation, ConnectionService.Instance);
                    SqlTask sqlTask = service.TaskManager.CreateTask<SqlTask>(taskMetadata);
                    Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
                    {
                        if (!makeTaskFail)
                        {
                            if (executionMode == TaskExecutionMode.Script || executionMode == TaskExecutionMode.ExecuteAndScript)
                            {
                                serviceHostMock.Verify(x => x.SendEvent(TaskStatusChangedNotification.Type,
                                           It.Is<TaskProgressInfo>(t => !string.IsNullOrEmpty(t.Script))), Times.AtLeastOnce());
                            }

                            //Verify if the table created if execution mode includes execute
                            bool expected = executionMode == TaskExecutionMode.Execute || executionMode == TaskExecutionMode.ExecuteAndScript;
                            Server serverToverfiy = CreateServerObject(connectionResult.ConnectionInfo);
                            bool actual = serverToverfiy.Databases[testDb.DatabaseName].Tables.Contains(taskOperation.TableName, "test");
                            Assert.Equal(expected, actual);
                        }
                        else
                        {
                            serviceHostMock.Verify(x => x.SendEvent(TaskStatusChangedNotification.Type,
                                           It.Is<TaskProgressInfo>(t => t.Status == SqlTaskStatus.Failed)), Times.AtLeastOnce());
                        }
                    });
                    await taskToVerify;
                }
                finally
                {
                    testDb.Cleanup();
                }

            }
        }
        protected TaskService CreateService()
        {
            CreateServiceProviderWithMinServices();

            // Create the service using the service provider, which will initialize dependencies
            return ServiceProvider.GetService<TaskService>();
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            TaskService service = new TaskService();
            service.TaskManager = new SqlTaskManager();
            return CreateProvider()
                .RegisterSingleService(service);
        }

        private Server CreateServerObject(ConnectionInfo connInfo   )
        {
            SqlConnection connection = null;
            DbConnection dbConnection = connInfo.AllConnections.First();
            ReliableSqlConnection reliableSqlConnection = dbConnection as ReliableSqlConnection;
            SqlConnection sqlConnection = dbConnection as SqlConnection;
            if (reliableSqlConnection != null)
            {
                connection = reliableSqlConnection.GetUnderlyingConnection();
            }
            else if (sqlConnection != null)
            {
                connection = sqlConnection;
            }
            return new Server(new ServerConnection(connection));
        }
    }
} 