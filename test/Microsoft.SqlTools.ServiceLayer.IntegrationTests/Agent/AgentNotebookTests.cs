//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
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

                AgentNotebooksResult result = await service.HandleAgentNotebooksRequest(new AgentNotebooksParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                });

                Assert.True(result.Success);
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
                CreateAgentNotebookResult result = await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = "garbargepath"
                });

                Assert.False(result.Success);
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
                CreateAgentNotebookResult createResult = await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                });

                Assert.True(createResult.Success);
                CreateAgentNotebookResult duplicateResult = await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                });
                Assert.False(duplicateResult.Success);
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
                CreateAgentNotebookResult result = await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                });
                Assert.True(result.Success);
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
                ResultStatus result = await service.HandleDeleteAgentNotebooksRequest(new DeleteAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook
                });
                Assert.True(result.Success);
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

                ResultStatus result = await service.HandleDeleteAgentNotebooksRequest(new DeleteAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook
                });
                //endpoint should error out
                Assert.False(result.Success);
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

                UpdateAgentNotebookResult result = await service.HandleUpdateAgentNotebookRequest(new UpdateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = AgentTestUtils.CreateTemplateNotebookFile()
                });
                // enpoint should error out
                Assert.False(result.Success);
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

                //calling the endpoint with a garbage path
                UpdateAgentNotebookResult result = await service.HandleUpdateAgentNotebookRequest(new UpdateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = "garbargepath"
                });
                //the enpoint should return false
                Assert.False(result.Success);

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
