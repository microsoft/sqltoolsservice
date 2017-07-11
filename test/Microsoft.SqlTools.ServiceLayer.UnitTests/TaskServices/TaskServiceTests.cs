//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{
    public class TaskServiceTests : ServiceTestBase
    {
        private TaskService service;
        private Mock<IProtocolEndpoint> serviceHostMock;
        private TaskMetadata taskMetaData = new TaskMetadata
        {
            ServerName = "server name",
            DatabaseName = "database name"
        };

        public TaskServiceTests()
        {
            serviceHostMock = new Mock<IProtocolEndpoint>();
            service = CreateService();
            service.InitializeService(serviceHostMock.Object);
        }

        [Fact]
        public async Task TaskListRequestErrorsIfParameterIsNull()
        {
            object errorResponse = null;
            var contextMock = RequestContextMocks.Create<ListTasksResponse>(null)
                                                 .AddErrorHandling((errorMessage, errorCode) => errorResponse = errorMessage);

            await service.HandleListTasksRequest(null, contextMock.Object);
            VerifyErrorSent(contextMock);
            Assert.True(((string)errorResponse).Contains("ArgumentNullException"));
        }

        [Fact]
        public void NewTaskShouldSendNotification()
        {
            serviceHostMock.AddEventHandling(TaskCreatedNotification.Type, null);
            serviceHostMock.AddEventHandling(TaskStatusChangedNotification.Type, null);
            DatabaseOperationStub operation = new DatabaseOperationStub();
            SqlTask sqlTask = service.TaskManager.CreateTask(taskMetaData, operation.FunctionToRun);
            sqlTask.Run();
           
            serviceHostMock.Verify(x => x.SendEvent(TaskCreatedNotification.Type,
                It.Is<TaskInfo>(t => t.TaskId == sqlTask.TaskId.ToString() && t.ProviderName == "MSSQL")), Times.Once());
            operation.Stop();
            Thread.Sleep(2000);

            serviceHostMock.Verify(x => x.SendEvent(TaskStatusChangedNotification.Type,
                It.Is<TaskProgressInfo>(t => t.TaskId == sqlTask.TaskId.ToString())), Times.AtLeastOnce());
        }

        [Fact]
        public async Task CancelTaskShouldCancelTheOperationAndSendNotification()
        {
            serviceHostMock.AddEventHandling(TaskCreatedNotification.Type, null);
            serviceHostMock.AddEventHandling(TaskStatusChangedNotification.Type, null);
            DatabaseOperationStub operation = new DatabaseOperationStub();
            SqlTask sqlTask = service.TaskManager.CreateTask(taskMetaData, operation.FunctionToRun, operation.FunctionToCancel);
            Task taskToVerify = sqlTask.RunAsync().ContinueWith(task =>
            {
                serviceHostMock.Verify(x => x.SendEvent(TaskStatusChangedNotification.Type,
                           It.Is<TaskProgressInfo>(t => t.Status == SqlTaskStatus.Canceled)), Times.AtLeastOnce());
            });
            CancelTaskParams cancelParams = new CancelTaskParams
            {
                TaskId = sqlTask.TaskId.ToString()
            };

            await RunAndVerify<bool>(
                test: (requestContext) => service.HandleCancelTaskRequest(cancelParams, requestContext),
                verify: ((result) =>
                {
                }));

            serviceHostMock.Verify(x => x.SendEvent(TaskCreatedNotification.Type,
                It.Is<TaskInfo>(t => t.TaskId == sqlTask.TaskId.ToString())), Times.Once());
            await taskToVerify;
        }


        [Fact]
        public async Task TaskListTaskShouldReturnAllTasks()
        {
            serviceHostMock.AddEventHandling(TaskCreatedNotification.Type, null);
            serviceHostMock.AddEventHandling(TaskStatusChangedNotification.Type, null);
            DatabaseOperationStub operation = new DatabaseOperationStub();
            SqlTask sqlTask = service.TaskManager.CreateTask(taskMetaData, operation.FunctionToRun);
            sqlTask.Run();
            ListTasksParams listParams = new ListTasksParams
            {
            };

            await RunAndVerify<ListTasksResponse>(
                test: (requestContext) => service.HandleListTasksRequest(listParams, requestContext),
                verify: ((result) =>
                {
                    Assert.True(result.Tasks.Any(x => x.TaskId == sqlTask.TaskId.ToString()));
                }));

            operation.Stop();
        }

        protected TaskService CreateService()
        {
            CreateServiceProviderWithMinServices();

            // Create the service using the service provider, which will initialize dependencies
            return ServiceProvider.GetService<TaskService>();
        }

        protected override RegisteredServiceProvider CreateServiceProviderWithMinServices()
        {
            return CreateProvider()
                .RegisterSingleService(new TaskService());
        }
    }
}
