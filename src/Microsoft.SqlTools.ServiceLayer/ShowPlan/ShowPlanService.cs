//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts;
using Microsoft.SqlTools.ShowPlan;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan
{
    /// <summary>
    /// Main class for Show Plan Service functionality
    /// </summary>
    public sealed class ShowPlanService
    {
        private static readonly Lazy<ShowPlanService> LazyInstance = new Lazy<ShowPlanService>(() => new ShowPlanService());

        public static ShowPlanService Instance => LazyInstance.Value;
        
        /// <summary>
        /// Initializes the Scripting Service instance
        /// </summary>
        /// <param name="serviceHost"></param>
        /// <param name="context"></param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(ShowPlanGetStatementInfoRequest.Type, HandleGetStatementInfoRequest);
        }      

        /// <summary>
        /// 
        /// </summary>
        internal static async Task HandleGetStatementInfoRequest(
            ShowPlanGetStatementInfoParams showPlanParams,
            RequestContext<ShowPlanGetStatementInfoResults> requestContext)
        {
            try
            {
                var parser = new ShowPlanXmlParser();
                var statementInfo = parser.ParseShowPlanXmlString(showPlanParams.PlanXml);

                await requestContext.SendResult(new ShowPlanGetStatementInfoResults
                {
                    StatementInfo = statementInfo
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendError(ex.ToString());
            }
        }
    }
}
