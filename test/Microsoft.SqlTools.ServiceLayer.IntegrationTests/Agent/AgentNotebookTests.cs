using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using System.Reflection;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
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
                var job = GetTestNotebookInfo("test1", "master");
                var job2 = GetTestNotebookInfo("test2", "master");
                Assembly assembly = Assembly.GetAssembly(typeof(AgentNotebookTests));
                using (Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Agent.NotebookResources.testNotebook.ipynb"))
                {
                    using (StreamReader reader = new StreamReader(scriptStream))
                    {
                        string testNotebook = reader.ReadToEnd();
                        
                        string tempTestNotebookFilePath = Path.GetTempFileName();
                        var tempFile = File.Create(tempTestNotebookFilePath);
                        var tempFileWriter = new StreamWriter(tempFile);
                        tempFileWriter.Write(testNotebook);
                        tempFileWriter.Close();
                        var context = new Mock<RequestContext<CreateAgentNotebookResult>>();
                        await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                        {
                            OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                            Notebook = job,
                            TemplateFilePath = tempTestNotebookFilePath
                        }, context.Object);
                        await service.HandleCreateAgentNotebookRequest(new CreateAgentNotebookParams()
                        {
                            OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                            Notebook = job,
                            TemplateFilePath = tempTestNotebookFilePath
                        }, context.Object);
                        var context2 = new Mock<RequestContext<AgentNotebooksResult>>();
                        await service.HandleAgentNotebooksRequest(new AgentNotebooksParams(){
                            OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                        }, context2.Object);
                        File.Delete(tempTestNotebookFilePath);
                    }
                }

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
                Owner = "sa"
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