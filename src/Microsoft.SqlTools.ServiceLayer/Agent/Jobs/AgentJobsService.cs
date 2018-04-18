//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Agent Jobs Service functionality
    /// </summary>
    public class AgentJobsService : IAgentJobsService
    {
        public ReturnResult InvokeJobAction(ConnectionInfo connInfo, string action, string jobName)
        {
            try
            {
                var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
                var serverConnection = new ServerConnection(sqlConnection);     
                var jobHelper = new JobHelper(serverConnection);
                jobHelper.JobName = jobName;
                switch(action)
                {
                    case "run":
                        jobHelper.Start();
                        break;
                    case "stop":
                        jobHelper.Stop();
                        break;
                    case "delete":
                        jobHelper.Delete();
                        break;
                    case "enable":
                        jobHelper.Enable(true);
                        break;
                    case "disable":
                        jobHelper.Enable(false);
                        break;
                    default:
                        break;
                }
                return new ReturnResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new ReturnResult 
                { 
                    Succeeded = false,
                    ErrorMessage = ex.ToString()
                };  
            }
        }

        public ReturnResult GetJobs(ConnectionInfo connInfo, out List<AgentJobInfo> jobs)
        {
            jobs = new List<AgentJobInfo>();
            try
            {                
                using (var sqlConnection = ConnectionService.OpenSqlConnection(connInfo))
                {
                    var serverConnection = new ServerConnection(sqlConnection);
                    var fetcher = new JobFetcher(serverConnection);
                    var filter = new JobActivityFilter();

                    Dictionary<Guid, JobProperties> jobProperties = fetcher.FetchJobs(filter);

                    var agentJobs = new List<AgentJobInfo>();
                    if (jobProperties != null)
                    {                    
                        foreach (var job in jobProperties.Values)
                        {
                            agentJobs.Add(JobUtilities.ConvertToAgentJobInfo(job));
                        }
                    }
                    return new ReturnResult { Succeeded = true };          
                }
            }
            catch (Exception ex)
            {
                return new ReturnResult 
                { 
                    Succeeded = false,
                    ErrorMessage = ex.ToString()
                };  
            }
        }

        public ReturnResult GetJobHistory(ConnectionInfo connInfo, string jobId, out List<AgentJobHistoryInfo> jobHistories)
        {
            jobHistories = new List<AgentJobHistoryInfo>();
            try
            {     
                ReturnResult returnValue = new ReturnResult();
                Tuple<SqlConnectionInfo, DataTable> tuple = CreateSqlConnection(connInfo, jobId);
                SqlConnectionInfo sqlConnInfo = tuple.Item1;
                DataTable dt = tuple.Item2;
                int count = dt.Rows.Count;

                var agentStepMap = new Dictionary<DateTime, List<AgentJobStep>>();
                for (int i = 0; i < count; ++i)
                {
                    var job = dt.Rows[i];
                    if (JobUtilities.IsStep(job, sqlConnInfo))
                    {
                        var agentJobStep = JobUtilities.ConvertToAgentJobStep(job, sqlConnInfo);
                        if (agentStepMap.ContainsKey(agentJobStep.RunDate))
                        {
                            agentStepMap[agentJobStep.RunDate].Add(agentJobStep);
                        }
                        else
                        {
                            var agentJobSteps = new List<AgentJobStep>();
                            agentJobSteps.Add(agentJobStep);
                            agentStepMap[agentJobStep.RunDate] = agentJobSteps;
                        }
                    }
                    else
                    {
                        var agentJobHistoryInfo = JobUtilities.ConvertToAgentJobHistoryInfo(job, sqlConnInfo);
                        jobHistories.Add(agentJobHistoryInfo);
                    }
                }

                foreach (AgentJobHistoryInfo agentJobHistoryInfo in jobHistories)
                {
                    if (agentStepMap.ContainsKey(agentJobHistoryInfo.RunDate))
                    { 
                        var agentStepList = agentStepMap[agentJobHistoryInfo.RunDate].ToList();
                        agentStepList.Sort(delegate (AgentJobStep s1, AgentJobStep s2) { return s1.StepId.CompareTo(s2.StepId); });
                        agentJobHistoryInfo.Steps = agentStepList.ToArray();
                    }
                }

                return new ReturnResult { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new ReturnResult 
                { 
                    Succeeded = false,
                    ErrorMessage = ex.ToString()
                };  
            }
        }

        private Tuple<SqlConnectionInfo, DataTable> CreateSqlConnection(ConnectionInfo connInfo, String jobId)
        {
            var sqlConnection = ConnectionService.OpenSqlConnection(connInfo);
            var serverConnection = new ServerConnection(sqlConnection);     
            var server = new Server(serverConnection);       
            var filter = new JobHistoryFilter(); 
            filter.JobID = new Guid(jobId);
            var dt = server.JobServer.EnumJobHistory(filter);
            var sqlConnInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);
            return new Tuple<SqlConnectionInfo, DataTable>(sqlConnInfo, dt);
        }        
    }
}
