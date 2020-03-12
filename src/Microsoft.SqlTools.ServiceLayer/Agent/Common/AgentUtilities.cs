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
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent 
{
    public class AgentUtilities
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
                Description = job.Description,
                CurrentExecutionStatus = (Contracts.JobExecutionStatus) job.CurrentExecutionStatus,
                LastRunOutcome = (Contracts.CompletionResult) job.LastRunOutcome,
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
                JobId = job.JobID != null ? job.JobID.ToString() : null,
                OperatorToEmail = job.OperatorToEmail,
                OperatorToPage = job.OperatorToPage,
                StartStepId = job.StartStepID,
                EmailLevel = job.EmailLevel,
                PageLevel = job.PageLevel,
                EventLogLevel = job.EventLogLevel,
                DeleteLevel = job.DeleteLevel,
                Owner = job.Owner
            };
        }

        public static AgentNotebookInfo ConvertToAgentNotebookInfo(JobProperties job)
        {
            return new AgentNotebookInfo(){
                Name = job.Name,
                Description = job.Description,
                CurrentExecutionStatus = (Contracts.JobExecutionStatus) job.CurrentExecutionStatus,
                LastRunOutcome = (Contracts.CompletionResult) job.LastRunOutcome,
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
                JobId = job.JobID != null ? job.JobID.ToString() : null,
                OperatorToEmail = job.OperatorToEmail,
                OperatorToPage = job.OperatorToPage,
                StartStepId = job.StartStepID,
                EmailLevel = job.EmailLevel,
                PageLevel = job.PageLevel,
                EventLogLevel = job.EventLogLevel,
                DeleteLevel = job.DeleteLevel,
                Owner = job.Owner
            };

        }

        internal static Contracts.CompletionResult ConvertToCompletionResult(SeverityClass severity)
        {
            switch (severity)
            {
                case (SeverityClass.Cancelled):
                    return Contracts.CompletionResult.Cancelled;
                case (SeverityClass.Error):
                    return Contracts.CompletionResult.Failed;
                case (SeverityClass.FailureAudit):
                    return Contracts.CompletionResult.Failed;
                case (SeverityClass.InProgress):
                    return Contracts.CompletionResult.InProgress;
                case (SeverityClass.Retry):
                    return Contracts.CompletionResult.Retry;
                case (SeverityClass.Success):
                    return Contracts.CompletionResult.Succeeded;
                case (SeverityClass.SuccessAudit):
                    return Contracts.CompletionResult.Succeeded;
                case (SeverityClass.Unknown):
                    return Contracts.CompletionResult.Unknown;
                default:
                    return Contracts.CompletionResult.Unknown;
            }
        }

        internal static AgentJobStep ConvertToAgentJobStep(JobStep step, LogSourceJobHistory.LogEntryJobHistory logEntry, string jobId)
        {
            AgentJobStepInfo stepInfo = new AgentJobStepInfo();
            stepInfo.JobId = jobId;
            stepInfo.JobName = logEntry.JobName;
            stepInfo.StepName = step.Name;
            stepInfo.SubSystem = step.SubSystem;
            stepInfo.Id = step.ID;
            stepInfo.FailureAction = step.OnFailAction;
            stepInfo.SuccessAction = step.OnSuccessAction;
            stepInfo.FailStepId = step.OnFailStep;
            stepInfo.SuccessStepId = step.OnSuccessStep;
            stepInfo.Command = step.Command;
            stepInfo.CommandExecutionSuccessCode = step.CommandExecutionSuccessCode;
            stepInfo.DatabaseName = step.DatabaseName;
            stepInfo.DatabaseUserName = step.DatabaseUserName;
            stepInfo.Server = step.Server;
            stepInfo.OutputFileName = step.OutputFileName;
            stepInfo.RetryAttempts = step.RetryAttempts;
            stepInfo.RetryInterval = step.RetryInterval;
            stepInfo.ProxyName = step.ProxyName;
            AgentJobStep jobStep = new AgentJobStep();
            jobStep.stepId = logEntry.StepID;
            jobStep.stepName = logEntry.StepName;
            jobStep.stepDetails = stepInfo;
            jobStep.message = logEntry.Message;
            jobStep.runDate = step.LastRunDate.ToString();
            jobStep.runStatus = ConvertToCompletionResult(logEntry.Severity);
            return jobStep;
        }

        internal static AgentJobStepInfo ConvertToAgentJobStepInfo(JobStep step, string jobId, string jobName)
        {
            AgentJobStepInfo stepInfo = new AgentJobStepInfo();
            stepInfo.JobId = jobId;
            stepInfo.JobName = jobName;
            stepInfo.StepName = step.Name;
            stepInfo.SubSystem = step.SubSystem;
            stepInfo.Id = step.ID;
            stepInfo.FailureAction = step.OnFailAction;
            stepInfo.SuccessAction = step.OnSuccessAction;
            stepInfo.FailStepId = step.OnFailStep;
            stepInfo.SuccessStepId = step.OnSuccessStep;
            stepInfo.Command = step.Command;
            stepInfo.CommandExecutionSuccessCode = step.CommandExecutionSuccessCode;
            stepInfo.DatabaseName = step.DatabaseName;
            stepInfo.DatabaseUserName = step.DatabaseUserName;
            stepInfo.Server = step.Server;
            stepInfo.OutputFileName = step.OutputFileName;
            stepInfo.RetryAttempts = step.RetryAttempts;
            stepInfo.RetryInterval = step.RetryInterval;
            stepInfo.ProxyName = step.ProxyName;
            return stepInfo;
        }
        

        internal static AgentScheduleInfo ConvertToAgentScheduleInfo(JobSchedule schedule)
        {
            AgentScheduleInfo scheduleInfo = new AgentScheduleInfo();
            scheduleInfo.Id = schedule.ID;
            scheduleInfo.Name = schedule.Name;
            scheduleInfo.JobName = " ";
            scheduleInfo.IsEnabled = schedule.IsEnabled;
            scheduleInfo.FrequencyTypes = (Contracts.FrequencyTypes) schedule.FrequencyTypes;
            scheduleInfo.FrequencySubDayTypes = (Contracts.FrequencySubDayTypes) schedule.FrequencySubDayTypes;
            scheduleInfo.FrequencySubDayInterval = schedule.FrequencySubDayInterval;
            scheduleInfo.FrequencyRelativeIntervals = (Contracts.FrequencyRelativeIntervals) schedule.FrequencyRelativeIntervals;
            scheduleInfo.FrequencyRecurrenceFactor = schedule.FrequencyRecurrenceFactor;
            scheduleInfo.FrequencyInterval = schedule.FrequencyInterval;
            scheduleInfo.DateCreated = schedule.DateCreated;
            scheduleInfo.ActiveStartTimeOfDay = schedule.ActiveStartTimeOfDay;
            scheduleInfo.ActiveStartDate = schedule.ActiveStartDate;
            scheduleInfo.ActiveEndTimeOfDay = schedule.ActiveEndTimeOfDay;
            scheduleInfo.ActiveEndDate = schedule.ActiveEndDate;
            scheduleInfo.JobCount = schedule.JobCount;
            scheduleInfo.ScheduleUid = schedule.ScheduleUid;
            var scheduleData = new JobScheduleData(schedule);
            scheduleInfo.Description = scheduleData.Description;
            return scheduleInfo;
        }

        internal static AgentAlertInfo[] ConvertToAgentAlertInfo(List<Alert> alerts)
        {
            var result = new List<AgentAlertInfo>();
            foreach(Alert alert in alerts)
            {
                AgentAlertInfo alertInfo = new AgentAlertInfo();
                alertInfo.Id = alert.ID;
                alertInfo.Name = alert.Name;
                alertInfo.DelayBetweenResponses = alert.DelayBetweenResponses;
                alertInfo.EventDescriptionKeyword = alert.EventDescriptionKeyword;
                alertInfo.EventSource = alert.EventSource;
                alertInfo.HasNotification = alert.HasNotification;
                alertInfo.IncludeEventDescription = (Contracts.NotifyMethods) alert.IncludeEventDescription;
                alertInfo.IsEnabled = alert.IsEnabled;
                alertInfo.JobId = alert.JobID.ToString();
                alertInfo.JobName = alert.JobName;
                alertInfo.LastOccurrenceDate = alert.LastOccurrenceDate.ToString();
                alertInfo.LastResponseDate = alert.LastResponseDate.ToString();
                alertInfo.MessageId = alert.MessageID;
                alertInfo.NotificationMessage = alert.NotificationMessage;
                alertInfo.OccurrenceCount = alert.OccurrenceCount;
                alertInfo.PerformanceCondition = alert.PerformanceCondition;
                alertInfo.Severity = alert.Severity;
                alertInfo.DatabaseName = alert.DatabaseName;
                alertInfo.CountResetDate = alert.CountResetDate.ToString();
                alertInfo.CategoryName = alert.CategoryName;
                alertInfo.AlertType = (Contracts.AlertType) alert.AlertType;
                alertInfo.WmiEventNamespace = alert.WmiEventNamespace;
                alertInfo.WmiEventQuery = alert.WmiEventQuery;
                result.Add(alertInfo);
            }
            return result.ToArray();
        }

        public static List<AgentJobHistoryInfo> ConvertToAgentJobHistoryInfo(List<ILogEntry> logEntries, DataRow jobRow, JobStepCollection steps) 
        {
            List<AgentJobHistoryInfo> jobs = new List<AgentJobHistoryInfo>();
            // get all the values for a job history
            foreach (ILogEntry entry in logEntries) 
            {
                // Make a new AgentJobHistoryInfo object
                var jobHistoryInfo = new AgentJobHistoryInfo();
                jobHistoryInfo.InstanceId = Convert.ToInt32(jobRow[UrnInstanceID], System.Globalization.CultureInfo.InvariantCulture);
                jobHistoryInfo.JobId = (Guid) jobRow[UrnJobId];
                var logEntry = entry as LogSourceJobHistory.LogEntryJobHistory;
                jobHistoryInfo.RunStatus = entry.Severity == SeverityClass.Error ? 0 : 1;
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
                foreach (LogSourceJobHistory.LogEntryJobHistory subEntry in entry.SubEntries)
                {
                    if (steps.Contains(subEntry.StepName))
                    {                                              
                        var jobId = jobRow[UrnJobId].ToString();
                        jobSteps.Add(AgentUtilities.ConvertToAgentJobStep(steps.ItemById(Convert.ToInt32(subEntry.StepID)), subEntry, jobId));
                    }
                }
                jobHistoryInfo.Steps = jobSteps.ToArray();
                jobs.Add(jobHistoryInfo);
            }
            return jobs;
        }

        public static List<AgentNotebookHistoryInfo> ConvertToAgentNotebookHistoryInfo(List<ILogEntry> logEntries, DataRow jobRow, JobStepCollection steps) 
        {
            List<AgentNotebookHistoryInfo> jobs = new List<AgentNotebookHistoryInfo>();
            // get all the values for a job history
            foreach (ILogEntry entry in logEntries) 
            {
                // Make a new AgentJobHistoryInfo object
                var jobHistoryInfo = new AgentNotebookHistoryInfo();
                jobHistoryInfo.InstanceId = Convert.ToInt32(jobRow[UrnInstanceID], System.Globalization.CultureInfo.InvariantCulture);
                jobHistoryInfo.JobId = (Guid) jobRow[UrnJobId];
                var logEntry = entry as LogSourceJobHistory.LogEntryJobHistory;
                jobHistoryInfo.RunStatus = entry.Severity == SeverityClass.Error ? 0 : 1;
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
                foreach (LogSourceJobHistory.LogEntryJobHistory subEntry in entry.SubEntries)
                {
                    if (steps.Contains(subEntry.StepName))
                    {                                              
                        var jobId = jobRow[UrnJobId].ToString();
                        jobSteps.Add(AgentUtilities.ConvertToAgentJobStep(steps.ItemById(Convert.ToInt32(subEntry.StepID)), logEntry, jobId));
                    }
                }
                jobHistoryInfo.Steps = jobSteps.ToArray();
                jobs.Add(jobHistoryInfo);
            }
            return jobs;
        }
    }
}