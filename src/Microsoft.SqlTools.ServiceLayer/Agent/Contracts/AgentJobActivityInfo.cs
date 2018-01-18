//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Profiler.Contracts
{
    /// <summary>
    /// Class that contains data for a single agent job activity
    /// </summary>
    public class AgentJobActivityInfo
    {
        List<JobProperties> Jobs { get; set; }
    }
}
