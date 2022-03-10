//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    public class GetExecutionPlanParams
    {
        public ExecutionPlanGraphFile GraphFile { get; set; }
    }

    public class GetExecutionPlanResult : ResultStatus
    {
        public List<ExecutionPlanGraph> Graphs { get; set; }
    }

    public class GetExecutionPlanRequest
    {
        public static readonly
        RequestType<GetExecutionPlanParams, GetExecutionPlanResult> Type = 
         RequestType<GetExecutionPlanParams, GetExecutionPlanResult>.Create("executionplan/getexecutionplan");
    }
}
