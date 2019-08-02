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
        public AgentJobStepInfo[] Steps { get; set; }
        public AgentScheduleInfo[] Schedules { get; set; }
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
    /// SQL Agent create Notebook params
    /// </summary>
    public class CreateAgentNotebookParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }
        public AgentNotebookInfo Notebook { get; set; }
        public string TemplateFilePath { get; set; }
    }

    /// <summary>
    /// SQL Agent create Notebook result
    /// </summary>
    public class CreateAgentNotebookResult  : ResultStatus
    {
        public AgentNotebookInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent create Notebook request type
    /// </summary>
    public class CreateAgentNotebookRequest
    {
        public static readonly
            RequestType<CreateAgentNotebookParams, CreateAgentNotebookResult> Type =
            RequestType<CreateAgentNotebookParams, CreateAgentNotebookResult>.Create("agent/createnotebook");
    }

    /// <summary>
    /// SQL Agent update Notebook params
    /// </summary>
    public class UpdateAgentNotebookParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }

        public string OriginalJobName { get; set; }

        public AgentNotebookInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent update Notebook result
    /// </summary>
    public class UpdateAgentNotebookResult : ResultStatus
    {
    }

    /// <summary>
    /// SQL Agent update Notebook request type
    /// </summary>
    public class UpdateAgentNotebookRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<UpdateAgentNotebookParams, UpdateAgentNotebookResult> Type =
            RequestType<UpdateAgentNotebookParams, UpdateAgentNotebookResult>.Create("agent/updatenotebook");
    }

    /// <summary>
    /// SQL Agent delete Notebook params
    /// </summary>
    public class DeleteAgentNotebookParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }

        public AgentNotebookInfo Job { get; set; }
    }

    /// <summary>
    /// SQL Agent delete Notebook request type
    /// </summary>
    public class DeleteAgentNotebookRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<DeleteAgentNotebookParams, ResultStatus> Type =
            RequestType<DeleteAgentNotebookParams, ResultStatus>.Create("agent/deletenotebook");
    }

    /// <summary>
    /// SQL Agent Notebook materialized params
    /// </summary>
    public class AgentNotebookMaterializedParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }
        public string TargetDatabase { get; set; }
        public int NotebookMaterializedID { get; set; }
    }

    /// <summary>
    /// SQL Agent Notebook materialized result
    /// </summary>
    public class AgentNotebookMaterializedResult : ResultStatus
    {
        public string NotebookMaterializedJson { get; set; }
    }

    /// <summary>
    /// SQL Agent Notebook materialized request type
    /// </summary>
    public class AgentNotebookMaterializedRequest
    {
        public static readonly
            RequestType<AgentNotebookMaterializedParams, AgentNotebookMaterializedResult> Type =
            RequestType<AgentNotebookMaterializedParams, AgentNotebookMaterializedResult>.Create("agent/notebookmaterialized");
    }

    /// <summary>
    /// SQL Agent Notebook templates params 
    /// </summary>
    public class AgentNotebookTemplateParams : TaskRequestDetails
    {
        public string OwnerUri { get; set; }
        public string TemplateId { get; set; }
        public string TargetDatabase { get; set; }
        
    }

    /// <summary>
    /// SQL Agent Notebook templates results 
    /// </summary>
    public class AgentNotebookTemplateResult : ResultStatus
    {
        public string NotebookTemplateJson { get; set; }
    }

    /// <summary>
    /// SQL Agent Notebook templates request type
    /// </summary>
    public class AgentNotebookTemplateRequest
    {
        public static readonly
            RequestType<AgentNotebookTemplateParams, AgentNotebookTemplateResult> Type =
            RequestType<AgentNotebookTemplateParams, AgentNotebookTemplateResult>.Create("agent/notebooktemplate");
    }




}