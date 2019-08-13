using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentNotebookTests
    {
        /// <summary>
        /// TestHandleUpdateAgentJobRequest
        /// </summary>
        [Fact]
        public async Task TestHandleAgentNotebooksRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var initialFetchContext = new Mock<RequestContext<AgentNotebooksResult>>();
                await service.HandleAgentNotebooksRequest(new AgentNotebooksParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, initialFetchContext.Object);
                initialFetchContext.VerifyAll();
                Assembly assembly = Assembly.GetAssembly(typeof(AgentNotebookTests));
                Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Agent.NotebookResources.TestNotebook.ipynb");
                StreamReader reader = new StreamReader(scriptStream);
                string testNotebookString = reader.ReadToEnd();
                string tempNotebookPath = System.IO.Path.GetTempFileName().Replace(".tmp", ".ipynb");
                if (!File.Exists(tempNotebookPath))
                {
                    File.WriteAllText(tempNotebookPath, testNotebookString);
                }
                var notebook = GetTestNotebookInfo("test1", "master");
                var createNotebookJobContext = new Mock<RequestContext<CreateAgentNotebookResult>>();
                await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook,
                    TemplateFilePath = tempNotebookPath
                }, createNotebookJobContext.Object);
                createNotebookJobContext.VerifyAll();
                initialFetchContext = new Mock<RequestContext<AgentNotebooksResult>>();
                await service.HandleAgentNotebooksRequest(new AgentNotebooksParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, initialFetchContext.Object);
                initialFetchContext.VerifyAll();
                var deleteNotebookJobContext = new Mock<RequestContext<ResultStatus>>();
                await service.HandleDeleteAgentNotebooksRequest(new DeleteAgentNotebookParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Notebook = notebook
                }, deleteNotebookJobContext.Object);
                initialFetchContext = new Mock<RequestContext<AgentNotebooksResult>>();
                await service.HandleAgentNotebooksRequest(new AgentNotebooksParams()
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                }, initialFetchContext.Object);
                initialFetchContext.VerifyAll();
            }
        }


        internal static AgentNotebookInfo GetTestNotebookInfo(string TestJobName, string TargetDatabase)
        {
            return new AgentNotebookInfo()
            {
                Name = TestJobName,
                Description = "Test job description",
                CurrentExecutionStatus = JobExecutionStatus.Executing,
                LastRunOutcome = CompletionResult.InProgress,
                CurrentExecutionStep = "Step 1",
                Enabled = false,
                HasTarget = false,
                HasSchedule = false,
                HasStep = false,
                Runnable = true,
                Category = "Cateory 1",
                CategoryId = 1,
                CategoryType = 1,
                LastRun = "today",
                NextRun = "tomorrow",
                JobId = Guid.NewGuid().ToString(),
                TargetDatabase = TargetDatabase,
                Owner = "sa",
                ExecuteDatabase = TargetDatabase
            };
        }

        internal static AgentJobStepInfo GetTestNotebookStepInfo(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobInfo job)
        {
            string stepName = "Exec-Notebook";
            var assembly = Assembly.GetAssembly(typeof(AgentNotebookTests));
            using (Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Agent.NotebookResources.NotebookJobScript.ps1"))
            {
                using (StreamReader reader = new StreamReader(scriptStream))
                {
                    string ExecNotebookScript = reader.ReadToEnd();
                    return new AgentJobStepInfo()
                    {
                        Id = 1,
                        JobName = job.Name,
                        StepName = stepName,
                        SubSystem = SqlServer.Management.Smo.Agent.AgentSubSystem.PowerShell,
                        Script = ExecNotebookScript,
                        DatabaseName = connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName,
                        DatabaseUserName = connectionResult.ConnectionInfo.ConnectionDetails.UserName,
                        Server = connectionResult.ConnectionInfo.ConnectionDetails.ServerName
                    };
                }
            }
        }
    }
}