//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    public class AgentJobHistoryInfo
    {
        public int InstanceId { get; set; }
        public string SqlMessageId { get; set; }
        public string Message { get; set; }
        public string StepId { get; set; }
        public string StepName { get; set; }
        public string SqlSeverity { get; set; }
        public Guid JobId { get; set; }
        public string JobName { get; set; }
        public int RunStatus { get; set; }
        public DateTime RunDate { get; set; }
        public string RunDuration { get; set; }
        public string OperatorEmailed { get; set; }
        public string OperatorNetsent { get; set; }
        public string OperatorPaged { get; set; }
        public string RetriesAttempted { get; set; }
        public string Server { get; set; }
        public AgentJobStep[] Steps { get; set; }
        public AgentScheduleInfo[] Schedules { get; set; }
        public AgentAlertInfo[] Alerts { get; set; }
    }

    public enum CompletionResult
    {
        Failed = 0,
        Succeeded = 1,
        Retry = 2,
        Cancelled = 3,
        InProgress = 4,
        Unknown = 5
    }

    public  class AgentJobStep 
    {
        public string jobId;
        public string stepId;
        public string stepName;
        public string message;
        public string runDate;
        public CompletionResult runStatus;
        public AgentJobStepInfo stepDetails;
	}

    /// <summary>
    /// a class for storing various properties of a agent notebook history
    /// </summary>
    public class AgentNotebookHistoryInfo : AgentJobHistoryInfo
    {
        public int MaterializedNotebookId { get; set; }
        public bool MaterializedNotebookPin { get; set; }
        public string MaterializedNotebookName { get; set; }
        public int MaterializedNotebookErrorFlag { get; set; }
        public string MaterializedNotebookErrorInfo { get; set; }
        public bool MaterializedNotebookDeleted { get; set; }
    }
}
