//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    public class AgentJobHistoryInfo
    {
        public int InstanceID { get; set; }
        public int SqlMessageID { get; set; }

        public string Message { get; set; }

        public int StepID { get; set; }

        public string StepName { get; set; }
        
        public int SqlSeverity { get; set; }

        public Guid JobID { get; set; }

        public string JobName { get; set; }

        public int RunStatus { get; set; }

        public DateTime RunDate { get; set; }

        public int RunDuration { get; set; }

        public string OperatorEmailed { get; set; }

        public string OperatorNetsent { get; set; }

        public string OperatorPaged { get; set; }

        public int RetriesAttempted { get; set; }

        public string Server { get; set; }
    }
    
}
