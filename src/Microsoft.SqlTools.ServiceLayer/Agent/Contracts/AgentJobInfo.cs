//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    public enum JobCompletionActionCondition
    {
        Never = 0,
        OnSuccess = 1,
        OnFailure = 2,
        Always = 3
    }

    public enum JobExecutionStatus
    {
        Executing = 1,
        WaitingForWorkerThread = 2,
        BetweenRetries = 3,
        Idle = 4,
        Suspended = 5,
        WaitingForStepToFinish = 6,
        PerformingCompletionAction = 7
    }

    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    public class AgentJobInfo
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
        public JobExecutionStatus CurrentExecutionStatus { get; set; }
        public CompletionResult LastRunOutcome { get; set; }
        public string CurrentExecutionStep { get; set; }
        public bool Enabled { get; set; }
        public bool HasTarget { get; set; }
        public bool HasSchedule { get; set; }
        public bool HasStep { get; set; }
        public bool Runnable { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
        public int CategoryType { get; set; }
        public string LastRun { get; set; }
        public string NextRun { get; set; }
        public string JobId { get; set; }
        public string OperatorToEmail { get; set; }
        public string OperatorToPage { get; set; }
        public int StartStepId { get; set; }
        public JobCompletionActionCondition EmailLevel { get; set; }
        public JobCompletionActionCondition PageLevel { get; set; }
        public JobCompletionActionCondition EventLogLevel { get; set; }
        public JobCompletionActionCondition DeleteLevel { get; set; }
        public AgentJobStepInfo[] JobSteps { get; set; }
        public AgentScheduleInfo[] JobSchedules { get; set; }
        public AgentAlertInfo[] Alerts { get; set; }
    }
}
