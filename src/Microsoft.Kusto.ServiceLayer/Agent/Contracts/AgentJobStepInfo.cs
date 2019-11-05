//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    public class AgentJobStepInfo
    {
        public string JobId { get; set; }

        public string JobName { get; set; }

        public string Script { get; set; }

        public string ScriptName { get; set; }

        public string StepName { get; set; }

        public AgentSubSystem SubSystem { get; set; }

        /// <summary>
        /// Current step id
        /// </summary>
        public int Id { get; set; }

        /// action to take if the step fails
        /// </summary>
        public StepCompletionAction FailureAction { get; set; }

        /// <summary>
        /// Action to take if the step succeeds
        /// </summary>
        public StepCompletionAction SuccessAction { get; set; }

        // note we will have either the id or step
        // for the steps to go to on failure
        /// <summary>
        /// step that will be executed on failure
        /// </summary>
        public int FailStepId { get; set; }
    
        /// <summary>
        /// step that will be executed on success
        /// </summary>
        public int SuccessStepId { get; set; }


        /// <summary>
        /// Command to execute
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// Success code for successful execution of the command
        /// </summary>
        public int CommandExecutionSuccessCode { get; set; }

        /// <summary>
        /// Database this step will execute against
        /// </summary>
        public string DatabaseName { get; set; }
    
        /// <summary>
        /// database user name this step will execute against
        /// </summary>
        public string DatabaseUserName { get; set; }

        /// <summary>
        /// Server to execute this step against
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// output file name
        /// </summary>
        public string OutputFileName { get; set; }

        /// <summary>
        /// indicates whether to append the output to a file
        /// </summary>
        public bool AppendToLogFile { get; set; }

        /// <summary>
        /// indicates whether to append the output to the step history
        /// </summary>
        public bool AppendToStepHist { get; set; }

        /// <summary>
        /// indicates whether to log to table
        /// </summary>
        public bool WriteLogToTable { get; set; }

        /// <summary>
        /// append the output to the table
        /// </summary>
        public bool AppendLogToTable { get; set; }

        /// <summary>
        /// number of rety attempts
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// retrey interval
        /// </summary>
        public int RetryInterval { get; set; }

        /// <summary>
        /// proxy name
        /// </summary>
        public string ProxyName { get; set; }
    }
}
