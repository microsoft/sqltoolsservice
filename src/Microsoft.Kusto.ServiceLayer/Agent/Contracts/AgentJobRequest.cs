//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.Kusto.ServiceLayer.TaskServices;
using Microsoft.Kusto.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.Kusto.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class AgentJobsParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobId { get; set; }
    }

    /// <summary>
    /// SQL Agent Job activity result
    /// </summary>
    public class AgentJobsResult : ResultStatus
    {
        public AgentJobInfo[] Jobs { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobsParams, AgentJobsResult> Type =
            RequestType<AgentJobsParams, AgentJobsResult>.Create("agent/jobs");
    }

    /// <summary>
    /// SQL Agent create Job params
    /// </summary>
    public class CreateAgentJobParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent create Job result
    /// </summary>
    public class CreateAgentJobResult : ResultStatus
    {
        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent create Alert request type
    /// </summary>
    public class CreateAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<CreateAgentJobParams, CreateAgentJobResult> Type =
            RequestType<CreateAgentJobParams, CreateAgentJobResult>.Create("agent/createjob");
    }

    /// <summary>
    /// SQL Agent update Job params
    /// </summary>
    public class UpdateAgentJobParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }

        public string OriginalJobName { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent update Job result
    /// </summary>
    public class UpdateAgentJobResult : ResultStatus
    {
    }

    /// <summary>
    /// SQL Agent update Job request type
    /// </summary>
    public class UpdateAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentJobParams, UpdateAgentJobResult> Type =
            RequestType<UpdateAgentJobParams, UpdateAgentJobResult>.Create("agent/updatejob");
    }

    /// <summary>
    /// SQL Agent delete Alert params
    /// </summary>
    public class DeleteAgentJobParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentJobInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Job request type
    /// </summary>
    public class DeleteAgentJobRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentJobParams, ResultStatus> Type =
            RequestType<DeleteAgentJobParams, ResultStatus>.Create("agent/deletejob");
    }

    /// <summary>
    /// SQL Agent Job history parameter
    /// </summary>
    public class AgentJobHistoryParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobId { get; set; }
        public string JobName { get; set; }
    }

    /// <summary>
    /// SQL Agent Job history result
    /// </summary>
    public class AgentJobHistoryResult : ResultStatus
    {
        public AgentJobHistoryInfo[] Histories { get; set; }
        public AgentJobStepInfo[] Steps { get; set; }
        public AgentScheduleInfo[] Schedules { get; set; }
        public AgentAlertInfo[] Alerts { get; set ;}
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobHistoryRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobHistoryParams, AgentJobHistoryResult> Type =
            RequestType<AgentJobHistoryParams, AgentJobHistoryResult>.Create("agent/jobhistory");
    }

    /// <summary>
    /// SQL Agent Job activity parameters
    /// </summary>
    public class AgentJobActionParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }

        public string JobName { get; set; }

        public string Action { get; set; }
    }

    /// <summary>
    /// SQL Agent Jobs request type
    /// </summary>
    public class AgentJobActionRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobActionParams, ResultStatus> Type =
            RequestType<AgentJobActionParams, ResultStatus>.Create("agent/jobaction");
    }

    /// <summary>
    /// SQL Agent Job Defaults params
    /// </summary>
    public class AgentJobDefaultsParams
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// SQL Agent Job Category class
    /// </summary>
    public class AgentJobCategory
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    /// <summary>
    /// SQL Agent Login
    /// </summary>
    public class AgentJobLogin 
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// SQL Agent Job Defaults result
    /// </summary>
    public class AgentJobDefaultsResult : ResultStatus
    {
        public string Owner { get; set; }

        public AgentJobCategory[] Categories { get; set; }
    }

    /// <summary>
    /// SQL Agent Job Defaults request type
    /// </summary>
    public class AgentJobDefaultsRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentJobDefaultsParams, AgentJobDefaultsResult> Type =
            RequestType<AgentJobDefaultsParams, AgentJobDefaultsResult>.Create("agent/jobdefaults");
    }
}
