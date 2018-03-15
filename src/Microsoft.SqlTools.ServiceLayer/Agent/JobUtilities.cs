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
        public const string urnJobName = "JobName";
        public const string urnJobId = "JobId";
        public const string urnRunStatus = "RunStatus";
        private const string urnInstanceID = "InstanceId";
        private const string urnSqlMessageID = "SqlMessageId";
        private const string urnMessage = "Message";
        private const string urnStepID = "StepId";
        private const string urnStepName = "StepName";
        private const string urnSqlSeverity = "SqlSeverity";
        private const string urnRunDate = "RunDate";
        private const string urnRunDuration = "RunDuration";
        private const string urnOperatorEmailed = "OperatorEmailed";
        private const string urnOperatorNetsent = "OperatorNetsent";
        private const string urnOperatorPaged = "OperatorPaged";
        private const string urnRetriesAttempted = "RetriesAttempted";
        private const string urnServer = "Server";
    
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

        public static AgentJobHistoryInfo ConvertToAgentJobHistoryInfo(DataRow jobRow, SqlConnectionInfo sqlConnInfo) 
        {
            // get all the values for a job history
            int instanceId = Convert.ToInt32(jobRow[urnInstanceID], System.Globalization.CultureInfo.InvariantCulture);
            int sqlMessageId = Convert.ToInt32(jobRow[urnSqlMessageID], System.Globalization.CultureInfo.InvariantCulture);
            string message = Convert.ToString(jobRow[urnMessage], System.Globalization.CultureInfo.InvariantCulture);
            int stepId = Convert.ToInt32(jobRow[urnStepID], System.Globalization.CultureInfo.InvariantCulture);
            string stepName = Convert.ToString(jobRow[urnStepName], System.Globalization.CultureInfo.InvariantCulture);
            int sqlSeverity = Convert.ToInt32(jobRow[urnSqlSeverity], System.Globalization.CultureInfo.InvariantCulture);
            Guid jobId = (Guid) jobRow[urnJobId];
            string jobName = Convert.ToString(jobRow[urnJobName], System.Globalization.CultureInfo.InvariantCulture);
            int runStatus = Convert.ToInt32(jobRow[urnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
            DateTime runDate = Convert.ToDateTime(jobRow[urnRunDate], System.Globalization.CultureInfo.InvariantCulture);
            int runDuration = Convert.ToInt32(jobRow[urnRunDuration], System.Globalization.CultureInfo.InvariantCulture);
            string operatorEmailed = Convert.ToString(jobRow[urnOperatorEmailed], System.Globalization.CultureInfo.InvariantCulture);
            string operatorNetsent = Convert.ToString(jobRow[urnOperatorNetsent], System.Globalization.CultureInfo.InvariantCulture);
            string operatorPaged = Convert.ToString(jobRow[urnOperatorPaged], System.Globalization.CultureInfo.InvariantCulture);
            int retriesAttempted = Convert.ToInt32(jobRow[urnRetriesAttempted], System.Globalization.CultureInfo.InvariantCulture);
            string server = Convert.ToString(jobRow[urnServer], System.Globalization.CultureInfo.InvariantCulture);

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
    }
}