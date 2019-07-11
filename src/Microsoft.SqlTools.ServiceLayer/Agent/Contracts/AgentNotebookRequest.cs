//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// SQL Agent Notebooks activity parameters
    /// </summary>
    public class AgentNotebooksParams : GeneralRequestDetails
    {
        public string OwnerUri {get; set;}
    
    }
    
    /// <summary>
    /// SQL Agent Notebook activity result
    /// </summary>
    public class AgentNotebooksResult : ResultStatus
    {
        public AgentNotebookInfo[] Notebooks {get; set;}
    }
    
    /// <summary>
    /// SQL Agent Notebook request type
    /// </summary>
    public class AgentNotebooksRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
        RequestType<AgentNotebooksParams, AgentNotebooksResult> Type = 
        RequestType<AgentNotebooksParams, AgentNotebooksResult>.Create("agent/notebooks");
    }
    
    /// <summary>
    /// SQL Agent Notebook Job Info Class
    /// </summary>
    public class AgentNotebookInfo
    {
        public AgentJobInfo Job {get; set;}
        public string Template {get; set;}
        public string TargetDatabase {get; set;}
    }
    
    /// <summary>
    /// SQL Agent Notebook history parameters
    /// </summary>
    public class AgentNotebookHistoryParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
        public string JobId { get; set; }
        public string TargetDatabase { get; set; }
        public string JobName { get; set; }
    }
    
    /// <summary>
    /// SQL Agent Notebook history results
    /// </summary>
    public class AgentNotebookHistoryResult : ResultStatus
    {
        public AgentNotebookHistoryInfo[] Histories { get; set; }
        public AgentJobStepInfo[] Step { get; set; }
        public AgentScheduleInfo[] Schedules { get; set; }
        public AgentAlertInfo[] Alerts { get; set;}
    }

    /// <summary>
    /// SQL Agent Notebook history request type
    /// <summary>
    public class AgentNotebookHistoryRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<AgentNotebookHistoryParams, AgentNotebookHistoryResult> Type =
            RequestType<AgentNotebookHistoryParams, AgentNotebookHistoryResult>.Create("agent/notebookhistory");
    }
    
    /// <summary>
    /// SQL Agent Notebook History Info Class
    /// </summary>
    public class AgentNotebookHistoryInfo
    {
        public AgentJobHistoryInfo History { get; set; }
        public string MaterializedNotebook { get; set; }
    }



}