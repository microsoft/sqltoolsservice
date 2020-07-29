using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentNotebookTests
    {
        /// <summary>
        /// Test case for fetch notebook jobs Request Handler
        /// </summary>
        [Test]
        public async Task TestHandleAgentNotebooksRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var fetchNotebooksContext = new Mock<RequestContext<AgentNotebooksResult>>();

                fetchNotebooksContext.Setup(x => x.SendResult(It.IsAny<AgentNotebooksResult>())).Returns(Task.FromResult(new object()));
                await service.HandleAgentNotebooksRequest(new AgentNotebooksParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, fetchNotebooksContext.Object);

                fetchNotebooksContext.Verify(x => x.SendResult(It.Is<AgentNotebooksResult>(p => p.Success == true)));

            }
        }

        /// <summary>
        /// Tests the create job helper function
        /// </summary>
        [Test]
        public async Task TestAgentNotebookCreateHelper()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                AgentNotebookInfo notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");
                Assert.AreEqual(false, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
                notebook = AgentTestUtils.SetupNotebookJob(connectionResult).Result;
                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
                await AgentTestUtils.CleanupNotebookJob(connectionResult, notebook);
            }
        }

        /// <summary>
        /// Tests the create job request handler with an invalid file path
        /// </summary>
        [Test]
        public async Task TestHandleCreateAgentNotebookRequestWithInvalidTemplatePath()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                AgentNotebookInfo notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");
                var createNotebookContext = new Mock<RequestContext<CreateAgentNotebookResult>>();
                createNotebookContext.Setup(x => x.SendResult(It.IsAny<CreateAgentNotebookResult>())).Returns(Task.FromResult(new object()));
                await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = "garbargepath"
                }, createNotebookContext.Object);

                createNotebookContext.Verify(x => x.SendResult(It.Is<CreateAgentNotebookResult>(p => p.Success == false)));
                Assert.AreEqual(false, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
            }
        }

        /// <summary>
        /// creating a job with duplicate name
        /// </summary>
        [Test]
        public async Task TestDuplicateJobCreation()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                AgentNotebookInfo notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");
                var createNotebookContext = new Mock<RequestContext<CreateAgentNotebookResult>>();
                createNotebookContext.Setup(x => x.SendResult(It.IsAny<CreateAgentNotebookResult>())).Returns(Task.FromResult(new object()));
                await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                }, createNotebookContext.Object);

                createNotebookContext.Verify(x => x.SendResult(It.Is<CreateAgentNotebookResult>(p => p.Success == true)));
                await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                }, createNotebookContext.Object);
                createNotebookContext.Verify(x => x.SendResult(It.Is<CreateAgentNotebookResult>(p => p.Success == false)));
                await AgentTestUtils.CleanupNotebookJob(connectionResult, notebook);
            }
        }

        /// <summary>
        /// Tests the create notebook job handler
        /// </summary>
        [Test]
        public async Task TestCreateAgentNotebookHandler()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                AgentNotebookInfo notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");
                var createNotebookContext = new Mock<RequestContext<CreateAgentNotebookResult>>();
                createNotebookContext.Setup(x => x.SendResult(It.IsAny<CreateAgentNotebookResult>())).Returns(Task.FromResult(new object()));
                await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                }, createNotebookContext.Object);
                createNotebookContext.Verify(x => x.SendResult(It.Is<CreateAgentNotebookResult>(p => p.Success == true)));
                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
                var createdNotebook = AgentTestUtils.GetNotebook(connectionResult, notebook.Name);
                await AgentTestUtils.CleanupNotebookJob(connectionResult, createdNotebook);
            }
        }

        /// <summary>
        /// Tests the delete notebook job handler
        /// </summary>
        [Test]
        public async Task TestDeleteAgentNotebookHandler()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                //creating a notebook job 
                AgentNotebookInfo notebook = AgentTestUtils.SetupNotebookJob(connectionResult).Result;
                //verifying it's getting created
                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
                //deleting the notebook job
                var deleteNotebookContext = new Mock<RequestContext<ResultStatus>>();
                deleteNotebookContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>())).Returns(Task.FromResult(new object()));
                await service.HandleDeleteAgentNotebooksRequest(new DeleteAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook
                }, deleteNotebookContext.Object);
                deleteNotebookContext.Verify(x => x.SendResult(It.Is<ResultStatus>(p => p.Success == true)));
                //verifying if the job is deleted
                Assert.AreEqual(false, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
            }
        }

        /// <summary>
        /// deleting a existing notebook job
        /// </summary>
        [Test]
        public async Task TestDeleteNonExistentJob()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                //getting a test notebook object
                var notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");

                var deleteNotebookContext = new Mock<RequestContext<ResultStatus>>();
                deleteNotebookContext.Setup(x => x.SendResult(It.IsAny<ResultStatus>())).Returns(Task.FromResult(new object()));
                await service.HandleDeleteAgentNotebooksRequest(new DeleteAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook
                }, deleteNotebookContext.Object);
                //endpoint should error out
                deleteNotebookContext.Verify(x => x.SendResult(It.Is<ResultStatus>(p => p.Success == false)));
            }
        }

        /// <summary>
        /// updating a non existing notebook job
        /// </summary>
        [Test]
        public async Task TestUpdateNonExistentJob()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                //getting a test notebook object
                AgentNotebookInfo notebook = AgentTestUtils.GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");

                var updateNotebookContext = new Mock<RequestContext<UpdateAgentNotebookResult>>();
                updateNotebookContext.Setup(x => x.SendResult(It.IsAny<UpdateAgentNotebookResult>())).Returns(Task.FromResult(new object()));
                await service.HandleUpdateAgentNotebookRequest(new UpdateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                }, updateNotebookContext.Object);
                // enpoint should error out
                updateNotebookContext.Verify(x => x.SendResult(It.Is<UpdateAgentNotebookResult>(p => p.Success == false)));
            }
        }

        /// <summary>
        /// update notebook handler with garbage path
        /// </summary>
        [Test]
        public async Task TestUpdateWithGarbagePath()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                //seting up a temp notebook job
                var notebook = AgentTestUtils.SetupNotebookJob(connectionResult).Result;
                //verifying that the notebook is created
                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));

                var updateNotebookContext = new Mock<RequestContext<UpdateAgentNotebookResult>>();
                updateNotebookContext.Setup(x => x.SendResult(It.IsAny<UpdateAgentNotebookResult>())).Returns(Task.FromResult(new object()));
                //calling the endpoint with a garbage path
                await service.HandleUpdateAgentNotebookRequest(new UpdateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = "garbargepath"
                }, updateNotebookContext.Object);
                //the enpoint should return false
                updateNotebookContext.Verify(x => x.SendResult(It.Is<UpdateAgentNotebookResult>(p => p.Success == false)));

                //cleaning up the job
                await AgentTestUtils.CleanupNotebookJob(connectionResult, notebook);
                Assert.AreEqual(false, AgentTestUtils.VerifyNotebook(connectionResult, notebook));
            }
        }

        [Test]
        public async Task TestDeletingUpdatedJob()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);

                //seting up a temp notebook job
                var notebook = AgentTestUtils.SetupNotebookJob(connectionResult).Result;
                //verifying that the notebook is created
                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));

                var originalName = notebook.Name;
                //Changing the notebookName 
                notebook.Name = "myTestNotebookJob" + Guid.NewGuid().ToString();

                Assert.AreEqual(false, AgentTestUtils.VerifyNotebook(connectionResult, notebook));

                await AgentNotebookHelper.UpdateNotebook(
                    service,
                    connectionResult.ConnectionInfo.OwnerUri,
                    originalName,
                    notebook,
                    null,
                    ManagementUtils.asRunType(0)
                );

                Assert.AreEqual(true, AgentTestUtils.VerifyNotebook(connectionResult, notebook));

                //cleaning up the job
                await AgentTestUtils.CleanupNotebookJob(connectionResult, notebook);
            }
        }
    }
}