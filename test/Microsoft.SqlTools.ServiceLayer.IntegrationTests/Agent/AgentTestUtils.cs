//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public static class AgentTestUtils
    {
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
                SubSystem = "T-SQL",
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
                Name = "Test Job",
                Owner = "sa",
                Description = "Test job description",
                CurrentExecutionStatus = 1,
                LastRunOutcome = 1,
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
                JobId = Guid.NewGuid().ToString()
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
                Id = 10,                    
                AccountName = "Test Proxy 2",
                CredentialName = "User",
                Description = "",
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
    }
}
