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
    public interface IAgentJobsService
    {
        ReturnResult InvokeJobAction(ConnectionInfo connInfo, string action, string jobName);

        ReturnResult GetJobs(ConnectionInfo connInfo, out List<AgentJobInfo> jobs);

        ReturnResult GetJobHistory(ConnectionInfo connInfo, string jobId, out List<AgentJobHistoryInfo> jobHistories);    
    }
}
