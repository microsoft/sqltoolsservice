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
        public NotebookJobInfo[] Notebooks {get; set;}
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
    public class NotebookJobInfo
    {
        public AgentJobInfo Job {get; set;}
        public string Template {get; set;}
        public string TargetDatabase {get; set;}
    }


}