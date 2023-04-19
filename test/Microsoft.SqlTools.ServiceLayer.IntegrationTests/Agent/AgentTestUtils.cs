//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public static class AgentTestUtils
    {
        public const string TestJobName = "Test Job";

        internal static AgentJobStepInfo GetTestJobStepInfo(
            TestConnectionResult connectionResult,
            AgentJobInfo job,
            string stepName = "Test Job Step1")
        {
            return new AgentJobStepInfo()
            {
                Id = 1,
                JobName = job.Name,
                StepName = stepName,
                SubSystem = SqlServer.Management.Smo.Agent.AgentSubSystem.TransactSql,
                Script = "SELECT @@VERSION",
                DatabaseName = connectionResult.ConnectionInfo.ConnectionDetails.DatabaseName,
                DatabaseUserName = connectionResult.ConnectionInfo.ConnectionDetails.UserName,
                Server = connectionResult.ConnectionInfo.ConnectionDetails.ServerName
            };
        }

        internal static AgentJobInfo GetTestJobInfo()
        {
            return new AgentJobInfo()
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
                Owner = "sa"
            };
        }

        internal static AgentOperatorInfo GetTestOperatorInfo()
        {
            return new AgentOperatorInfo()
            {
                Id = 10,
                Name = "Joe DBA",
                EmailAddress = "test@aol.com"
            };
        }

        internal static AgentProxyInfo GetTestProxyInfo()
        {
            return new AgentProxyInfo()
            {
                AccountName = "Test Proxy",
                CredentialName = ObjectManagementTestUtils.TestCredentialName,
                Description = "Test proxy description",
                IsEnabled = true
            };
        }

        internal static AgentScheduleInfo GetTestScheduleInfo()
        {
            return new AgentScheduleInfo()
            {
                Name = "Test Schedule",
                JobName = TestJobName,
                IsEnabled = true
            };
        }

        internal static async Task CreateAgentJob(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobInfo job)
        {
            var context = new Mock<RequestContext<CreateAgentJobResult>>();
            await service.HandleCreateAgentJobRequest(new CreateAgentJobParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Job = job
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateAgentJob(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobInfo job)
        {
            job.Description = "Update job description";
            var context = new Mock<RequestContext<UpdateAgentJobResult>>();
            await service.HandleUpdateAgentJobRequest(new UpdateAgentJobParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Job = job
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteAgentJob(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobInfo job,
            bool verify = true)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteAgentJobRequest(new DeleteAgentJobParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Job = job
            }, context.Object);

            if (verify)
            {
                context.VerifyAll();
            }
        }

        internal static async Task CreateAgentJobStep(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobStepInfo stepInfo)
        {
            var context = new Mock<RequestContext<CreateAgentJobStepResult>>();
            await service.HandleCreateAgentJobStepRequest(new CreateAgentJobStepParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Step = stepInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateAgentJobStep(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobStepInfo stepInfo)
        {
            var context = new Mock<RequestContext<UpdateAgentJobStepResult>>();
            await service.HandleUpdateAgentJobStepRequest(new UpdateAgentJobStepParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Step = stepInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteAgentJobStep(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentJobStepInfo stepInfo)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteAgentJobStepRequest(new DeleteAgentJobStepParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Step = stepInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task CreateAgentOperator(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentOperatorInfo operatorInfo)
        {
            var context = new Mock<RequestContext<AgentOperatorResult>>();
            await service.HandleCreateAgentOperatorRequest(new CreateAgentOperatorParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Operator = operatorInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateAgentOperator(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentOperatorInfo operatorInfo)
        {
            var context = new Mock<RequestContext<AgentOperatorResult>>();
            await service.HandleUpdateAgentOperatorRequest(new UpdateAgentOperatorParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Operator = operatorInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteAgentOperator(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentOperatorInfo operatorInfo)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteAgentOperatorRequest(new DeleteAgentOperatorParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Operator = operatorInfo
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task CreateAgentProxy(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentProxyInfo proxy)
        {
            var context = new Mock<RequestContext<AgentProxyResult>>();
            await service.HandleCreateAgentProxyRequest(new CreateAgentProxyParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Proxy = proxy
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateAgentProxy(
            AgentService service,
            TestConnectionResult connectionResult,
            string originalProxyName,
            AgentProxyInfo proxy)
        {
            var context = new Mock<RequestContext<AgentProxyResult>>();
            await service.HandleUpdateAgentProxyRequest(new UpdateAgentProxyParams()
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                OriginalProxyName = originalProxyName,
                Proxy = proxy
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteAgentProxy(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentProxyInfo proxy)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteAgentProxyRequest(new DeleteAgentProxyParams()
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Proxy = proxy
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task CreateAgentSchedule(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentScheduleInfo schedule)
        {
            var context = new Mock<RequestContext<AgentScheduleResult>>();
            await service.HandleCreateAgentScheduleRequest(new CreateAgentScheduleParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Schedule = schedule
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task UpdateAgentSchedule(
           AgentService service,
           TestConnectionResult connectionResult,
           string originalScheduleName,
           AgentScheduleInfo schedule)
        {
            var context = new Mock<RequestContext<AgentScheduleResult>>();
            await service.HandleUpdateAgentScheduleRequest(new UpdateAgentScheduleParams()
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                OriginalScheduleName = originalScheduleName,
                Schedule = schedule
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task DeleteAgentSchedule(
            AgentService service,
            TestConnectionResult connectionResult,
            AgentScheduleInfo schedule)
        {
            var context = new Mock<RequestContext<ResultStatus>>();
            await service.HandleDeleteAgentScheduleRequest(new DeleteAgentScheduleParams()
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                Schedule = schedule
            }, context.Object);
            context.VerifyAll();
        }

        internal static async Task<AgentAlertInfo[]> GetAgentAlerts(string connectionUri)
        {
            var requestParams = new AgentAlertsParams()
            {
                OwnerUri = connectionUri
            };

            var requestContext = new Mock<RequestContext<AgentAlertsResult>>();

            AgentAlertInfo[] agentAlerts = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<AgentAlertsResult>()))
                 .Callback<AgentAlertsResult>(r => agentAlerts = r.Alerts);

            AgentService service = new AgentService();
            await service.HandleAgentAlertsRequest(requestParams, requestContext.Object);
            return agentAlerts;
        }

        public static async Task<AgentJobInfo> SetupJob(TestConnectionResult connectionResult)
        {
            var service = new AgentService();
            var job = GetTestJobInfo();
            await DeleteAgentJob(service, connectionResult, job);
            await CreateAgentJob(service, connectionResult, job);
            return job;
        }

        public static async Task CleanupJob(
            TestConnectionResult connectionResult,
            AgentJobInfo job)
        {
            var service = new AgentService();
            await DeleteAgentJob(service, connectionResult, job);
        }



        public static async Task<AgentNotebookInfo> SetupNotebookJob(
            TestConnectionResult connectionResult,
            AgentNotebookInfo notebook = null)
        {
            var service = new AgentService();
            notebook ??= GetTestNotebookInfo("myTestNotebookJob" + Guid.NewGuid().ToString(), "master");
            string tempNotebookPath = CreateTemplateNotebookFile();

            await AgentNotebookHelper.CreateNotebook(
               service,
               connectionResult.ConnectionInfo.OwnerUri,
               notebook,
               tempNotebookPath,
               ManagementUtils.asRunType(0)
           );

            var createdNotebook = GetNotebook(connectionResult, notebook.Name);
            File.Delete(tempNotebookPath);
            return createdNotebook;
        }

        public static async Task CleanupNotebookJob(TestConnectionResult connectionResult, AgentNotebookInfo notebook)
        {
            var service = new AgentService();
            await AgentNotebookHelper.DeleteNotebook(
                service,
                connectionResult.ConnectionInfo.OwnerUri,
                notebook,
                ManagementUtils.asRunType(0)
            );
        }

        public static AgentNotebookInfo GetNotebook(TestConnectionResult connectionResult, string name){
             var notebookList = AgentNotebookHelper.GetAgentNotebooks(connectionResult.ConnectionInfo).Result;
             foreach(AgentNotebookInfo n in notebookList)
             {
                 if(n.Name == name)
                 {
                      return n;
                 }
             }
             return null;
        }


        public static bool VerifyNotebook(TestConnectionResult connectionResult, AgentNotebookInfo notebook)
        {
            var notebookList = AgentNotebookHelper.GetAgentNotebooks(connectionResult.ConnectionInfo).Result;
            foreach (AgentNotebookInfo n in notebookList)
            {
                if (NotebookObjectEquals(notebook, n))
                {
                    return true;
                }
            }
            return false;
        }
        static bool NotebookObjectEquals(AgentNotebookInfo expectedNotebook, AgentNotebookInfo actualNotebook)
        {
            return (
                expectedNotebook.Name == actualNotebook.Name
                &&
                expectedNotebook.Description == actualNotebook.Description
            );
        }

        internal static string CreateTemplateNotebookFile()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(AgentNotebookTests));
            Stream scriptStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Agent.NotebookResources.TestNotebook.ipynb");
            StreamReader reader = new StreamReader(scriptStream);
            string testNotebookString = reader.ReadToEnd();
            string tempNotebookPath = System.IO.Path.GetTempFileName().Replace(".tmp", ".ipynb");
            File.WriteAllText(tempNotebookPath, testNotebookString);
            return tempNotebookPath;
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
                JobId = new Guid().ToString(),
                TargetDatabase = TargetDatabase,
                Owner = "sa",
                ExecuteDatabase = TargetDatabase,
                JobSchedules = new AgentScheduleInfo[0],
                Alerts = new AgentAlertInfo[0]
            };
        }

    }
}
