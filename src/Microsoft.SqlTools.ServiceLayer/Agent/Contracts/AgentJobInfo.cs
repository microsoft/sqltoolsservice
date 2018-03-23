//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    public class AgentJobInfo
    {
        public string Name { get; set; }
        public int CurrentExecutionStatus { get; set; }
        public int LastRunOutcome { get; set; }
        public string CurrentExecutionStep { get; set; }
        public bool Enabled { get; set; }
        public bool HasTarget { get; set; }
        public bool HasSchedule { get; set; }
        public bool HasStep { get; set; }
        public bool Runnable { get; set; }
        public string Category { get; set; }
        public int CategoryId { get; set; }
        public int CategoryType { get; set; }
        public string LastRun { get; set; }
        public string NextRun { get; set; }
        public string JobId { get; set; }
    }
    
}
