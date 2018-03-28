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

        public static Boolean IsStep(DataRow row, SqlConnectionInfo sqlConnectionInfo)
        {
            int stepId = Convert.ToInt32(row[UrnStepID], System.Globalization.CultureInfo.InvariantCulture);
            if (stepId != 0)
            {
                return true;
            }
            return false;
        }

        public static AgentJobHistoryInfo ConvertToAgentJobHistoryInfo(DataRow jobRow, SqlConnectionInfo sqlConnInfo) 
        {
            // get all the values for a job history
            int instanceId = Convert.ToInt32(jobRow[UrnInstanceID], System.Globalization.CultureInfo.InvariantCulture);
            int sqlMessageId = Convert.ToInt32(jobRow[UrnSqlMessageID], System.Globalization.CultureInfo.InvariantCulture);
            string message = Convert.ToString(jobRow[UrnMessage], System.Globalization.CultureInfo.InvariantCulture);
            int stepId = Convert.ToInt32(jobRow[UrnStepID], System.Globalization.CultureInfo.InvariantCulture);
            string stepName = Convert.ToString(jobRow[UrnStepName], System.Globalization.CultureInfo.InvariantCulture);
            int sqlSeverity = Convert.ToInt32(jobRow[UrnSqlSeverity], System.Globalization.CultureInfo.InvariantCulture);
            Guid jobId = (Guid) jobRow[UrnJobId];
            string jobName = Convert.ToString(jobRow[UrnJobName], System.Globalization.CultureInfo.InvariantCulture);
            int runStatus = Convert.ToInt32(jobRow[UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
            DateTime runDate = Convert.ToDateTime(jobRow[UrnRunDate], System.Globalization.CultureInfo.InvariantCulture);
            int runDuration = Convert.ToInt32(jobRow[UrnRunDuration], System.Globalization.CultureInfo.InvariantCulture);
            string operatorEmailed = Convert.ToString(jobRow[UrnOperatorEmailed], System.Globalization.CultureInfo.InvariantCulture);
            string operatorNetsent = Convert.ToString(jobRow[UrnOperatorNetsent], System.Globalization.CultureInfo.InvariantCulture);
            string operatorPaged = Convert.ToString(jobRow[UrnOperatorPaged], System.Globalization.CultureInfo.InvariantCulture);
            int retriesAttempted = Convert.ToInt32(jobRow[UrnRetriesAttempted], System.Globalization.CultureInfo.InvariantCulture);
            string server = Convert.ToString(jobRow[UrnServer], System.Globalization.CultureInfo.InvariantCulture);

            // initialize logger
            var t = new LogSourceJobHistory(jobName, sqlConnInfo, null, runStatus, jobId, null);
            var tlog = t as ILogSource;
            tlog.Initialize();

            // return new job history object as a result
            var jobHistoryInfo = new AgentJobHistoryInfo();    
            jobHistoryInfo.InstanceId = instanceId;
            jobHistoryInfo.SqlMessageId = sqlMessageId;
            jobHistoryInfo.Message = message;
            jobHistoryInfo.StepId = stepId;
            jobHistoryInfo.StepName = stepName;
            jobHistoryInfo.SqlSeverity = sqlSeverity;
            jobHistoryInfo.JobId = jobId;
            jobHistoryInfo.JobName = jobName;
            jobHistoryInfo.RunStatus = runStatus;
            jobHistoryInfo.RunDate = runDate;
            jobHistoryInfo.RunDuration = runDuration;
            jobHistoryInfo.OperatorEmailed = operatorEmailed;
            jobHistoryInfo.OperatorNetsent = operatorNetsent;
            jobHistoryInfo.OperatorPaged = operatorPaged;
            jobHistoryInfo.RetriesAttempted = retriesAttempted;
            jobHistoryInfo.Server = server;
            return jobHistoryInfo;
        }

        public static AgentJobStep ConvertToAgentJobStep(DataRow jobRow, SqlConnectionInfo sqlConnInfo)
        {
            int stepId = Convert.ToInt32(jobRow[UrnStepID], System.Globalization.CultureInfo.InvariantCulture);
            string stepName = Convert.ToString(jobRow[UrnStepName], System.Globalization.CultureInfo.InvariantCulture);
            string message = Convert.ToString(jobRow[UrnMessage], System.Globalization.CultureInfo.InvariantCulture);
            DateTime runDate = Convert.ToDateTime(jobRow[UrnRunDate], System.Globalization.CultureInfo.InvariantCulture);
            AgentJobStep step = new AgentJobStep();
            step.StepId = stepId;
            step.StepName = stepName;
            step.Message = message;
            step.RunDate = runDate;
            return step;
        }
    }
}