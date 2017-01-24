//------------------------------------------------------------------------------
// <copyright file="ShowPlanType.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
