//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    [System.Flags]
    internal enum ShowPlanType
    {
        None = 0x0,
        ActualExecutionShowPlan = 0x1,
        ActualXmlShowPlan = 0x2,
        EstimatedExecutionShowPlan = 0x4,
        EstimatedXmlShowPlan = 0x8,
        AllXmlShowPlan = ActualXmlShowPlan | EstimatedXmlShowPlan,
        AllExecutionShowPlan = ActualExecutionShowPlan | EstimatedExecutionShowPlan,
        AllShowPlan = AllExecutionShowPlan | AllXmlShowPlan
    }
}
