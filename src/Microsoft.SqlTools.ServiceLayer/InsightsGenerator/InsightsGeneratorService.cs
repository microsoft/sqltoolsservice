//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.InsightsGenerator.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.InsightsGenerator
{
    /// <summary>
    /// Service responsible for securing credentials in a platform-neutral manner. This provides
    /// a generic API for read, save and delete credentials
    /// </summary>
    public class InsightsGeneratorService
    {
        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<InsightsGeneratorService> instance
            = new Lazy<InsightsGeneratorService>(() => new InsightsGeneratorService());

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static InsightsGeneratorService Instance
        {
            get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            // Insights Generatoe request handlers
            serviceHost.SetRequestHandler(QueryInsightsGeneratorRequest.Type, HandleQueryInsightGeneratorRequest);
        }

        internal async Task HandleQueryInsightGeneratorRequest(QueryInsightsGeneratorParams parameters, RequestContext<InsightsGeneratorResult> requestContext)
        {
            QueryExecutionService service = QueryExecutionService.Instance;

            QueryExecution.Contracts.SubsetParams subsetParams = new QueryExecution.Contracts.SubsetParams
            {
                OwnerUri = parameters.OwnerUri
            };

            var result = (await service.InterServiceResultSubset(subsetParams)).Rows;

            Microsoft.InsightsGenerator.Workflow insightWorkFlowInstance = Microsoft.InsightsGenerator.Workflow.Instance();

            Microsoft.InsightsGenerator.DataArray dataArray = new Microsoft.InsightsGenerator.DataArray();

            try
            {
                string[] insightText = insightWorkFlowInstance.IngestRules(dataArray);

                await requestContext.SendResult(new InsightsGeneratorResult()
                {
                    InsightsText = insightText,
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (Exception ex)
            {
                await requestContext.SendResult(new InsightsGeneratorResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
