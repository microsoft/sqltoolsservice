//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent 
{
    public class JobUtilities
    {
        public const string UrnJobName = "JobName";
        public const string UrnJobId = "JobId";
        public const string UrnRunStatus = "RunStatus";
        public const string UrnInstanceID = "InstanceId";
        public const string UrnSqlMessageID = "SqlMessageId";
        public const string UrnMessage = "Message";
        public const string UrnStepID = "StepId";
        public const string UrnStepName = "StepName";
        public const string UrnSqlSeverity = "SqlSeverity";
        public const string UrnRunDate = "RunDate";
        public const string UrnRunDuration = "RunDuration";
        public const string UrnOperatorEmailed = "OperatorEmailed";
        public const string UrnOperatorNetsent = "OperatorNetsent";
        public const string UrnOperatorPaged = "OperatorPaged";
        public const string UrnRetriesAttempted = "RetriesAttempted";
        public const string UrnServer = "Server";
        internal const string UrnServerTime = "CurrentDate";
    
        public static AgentJobInfo ConvertToAgentJobInfo(JobProperties job)
        {
            return new AgentJobInfo
            {
                Name = job.Name,
                CurrentExecutionStatus = job.CurrentExecutionStatus,
                LastRunOutcome = job.LastRunOutcome,
                CurrentExecutionStep = job.CurrentExecutionStep,
                Enabled = job.Enabled,
                HasTarget = job.HasTarget,
                HasSchedule = job.HasSchedule,
                HasStep = job.HasStep, 
                Runnable = job.Runnable,
                Category = job.Category,
                CategoryId = job.CategoryID,
                CategoryType = job.CategoryType,
                LastRun = job.LastRun != null ? job.LastRun.ToString() : string.Empty,
                NextRun = job.NextRun != null ? job.NextRun.ToString() : string.Empty,
                JobId = job.JobID != null ? job.JobID.ToString() : null
            };
        }

        public static AgentJobStep ConvertToAgentJobStep(ILogEntry logEntry, DataRow jobRow)
        {
            var entry = logEntry as LogSourceJobHistory.LogEntryJobHistory;
            string stepId = entry.StepID;
            string stepName = entry.StepName;
            string message = entry.Message;
            DateTime runDate = logEntry.PointInTime;
            int runStatus = Convert.ToInt32(jobRow[UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
            AgentJobStep step = new AgentJobStep();
            step.StepId = stepId;
            step.StepName = stepName;
            step.Message = message;
            step.RunDate = runDate;
            step.RunStatus = runStatus;
            return step;
        }

        public static List<AgentJobHistoryInfo> ConvertToAgentJobHistoryInfo(List<ILogEntry> logEntries, DataRow jobRow) 
        {
            List<AgentJobHistoryInfo> jobs = new List<AgentJobHistoryInfo>();
            // get all the values for a job history
            foreach (ILogEntry entry in logEntries) 
            {
                // Make a new AgentJobHistoryInfo object
                var jobHistoryInfo = new AgentJobHistoryInfo();
                jobHistoryInfo.InstanceId = Convert.ToInt32(jobRow[UrnInstanceID], System.Globalization.CultureInfo.InvariantCulture);
                jobHistoryInfo.RunStatus = Convert.ToInt32(jobRow[UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
                jobHistoryInfo.JobId = (Guid) jobRow[UrnJobId];
                var logEntry = entry as LogSourceJobHistory.LogEntryJobHistory;
                jobHistoryInfo.SqlMessageId = logEntry.SqlMessageID;
                jobHistoryInfo.Message = logEntry.Message;
                jobHistoryInfo.StepId = logEntry.StepID;
                jobHistoryInfo.StepName = logEntry.StepName;
                jobHistoryInfo.SqlSeverity = logEntry.SqlSeverity;
                jobHistoryInfo.JobName = logEntry.JobName;
                jobHistoryInfo.RunDate = entry.PointInTime;
                jobHistoryInfo.RunDuration = logEntry.Duration;
                jobHistoryInfo.OperatorEmailed = logEntry.OperatorEmailed;
                jobHistoryInfo.OperatorNetsent = logEntry.OperatorNetsent;
                jobHistoryInfo.OperatorPaged = logEntry.OperatorPaged;
                jobHistoryInfo.RetriesAttempted = logEntry.RetriesAttempted;
                jobHistoryInfo.Server = logEntry.Server;

                // Add steps to the job if any
                var jobSteps = new List<AgentJobStep>();
                if (entry.CanLoadSubEntries)
                {
                    foreach (ILogEntry step in entry.SubEntries)
                    {
                        jobSteps.Add(JobUtilities.ConvertToAgentJobStep(step, jobRow));
                    }
                }
                jobHistoryInfo.Steps = jobSteps.ToArray();
                jobs.Add(jobHistoryInfo);
            }
            return jobs;
        }
    }
}