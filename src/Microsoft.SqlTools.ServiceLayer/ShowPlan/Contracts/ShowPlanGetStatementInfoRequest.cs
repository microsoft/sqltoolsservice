//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ShowPlan;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    public class ShowPlanGetStatementInfoParams
    {
        public string PlanXml { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ShowPlanGetStatementInfoResults
    {
        public List<ShowPlanXmlStatement> StatementInfo { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ShowPlanGetStatementInfoRequest
    {
        public static readonly RequestType<ShowPlanGetStatementInfoParams, ShowPlanGetStatementInfoResults> Type = 
            RequestType<ShowPlanGetStatementInfoParams, ShowPlanGetStatementInfoResults>.Create("showplan/getstatementinfo");
    }
}
