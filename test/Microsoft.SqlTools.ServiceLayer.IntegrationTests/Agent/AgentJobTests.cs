//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Agent
{
    public class AgentJobTests
    {
        /// <summary>
        /// TestHandleUpdateAgentJobStepRequest
        /// </summary>
        [Fact]
        public async Task TestHandleUpdateAgentJobStepRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var createContext = new Mock<RequestContext<CreateAgentJobStepResult>>();
                var service = new AgentService();
                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                await service.HandleCreateAgentJobStepRequest(new CreateAgentJobStepParams
                {
                    OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                    Step = new AgentJobStepInfo()
                    {
                        // JobId = Guid.NewGuid().ToString(),
                        Script = @"c:\xplat\test.sql",
                        ScriptName = "Test Script",

                        
                    }
                }, createContext.Object);
                createContext.VerifyAll();
            }
        }
    }
}

/*
            this.originalName = source.originalName;
            this.currentName = source.currentName;
            this.alreadyCreated = source.alreadyCreated;
            this.deleted = source.deleted;
            this.command = source.command;
            this.commandExecutionSuccessCode = source.commandExecutionSuccessCode;
            this.databaseName = source.databaseName;
            this.databaseUserName = source.databaseUserName;
            this.server = source.server;
            this.id = source.id;
            this.originalId = source.originalId;
            this.failureAction = source.failureAction;
            this.failStep = source.failStep;
            this.failStepId = source.failStepId;
            this.successAction = source.successAction;
            this.successStep = source.successStep;
            this.successStepId = source.successStepId;
            this.priority = source.priority;
            this.outputFileName = source.outputFileName;
            this.appendToLogFile = source.appendToLogFile;
            this.appendToStepHist = source.appendToStepHist;
            this.writeLogToTable = source.writeLogToTable;
            this.appendLogToTable = source.appendLogToTable;
            this.retryAttempts = source.retryAttempts;
            this.retryInterval = source.retryInterval;
            this.subSystem = source.subSystem;
            this.proxyName = source.proxyName;
            this.urn = source.urn;
            this.parent = source.parent;

 */
