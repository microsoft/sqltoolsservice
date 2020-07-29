//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.InsightsGenerator.Contracts;
using Microsoft.InsightsGenerator;

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
            
            string[][] transposedRows = new string[parameters.Data.Columns.Length][];

            for (int column = 0; column < parameters.Data.Columns.Length; column++)
            {
                transposedRows[column] = new string[parameters.Data.Rows.Length];
                for (int row = 0; row < parameters.Data.Rows.Length; row++)
                {
                    transposedRows[column][row] = parameters.Data.Rows[row][column];
                }
            }

            Microsoft.InsightsGenerator.DataArray dataArray = new Microsoft.InsightsGenerator.DataArray()
            {
                ColumnNames = parameters.Data.Columns,
                Cells = transposedRows
            };

            Workflow insightWorkFlow = Workflow.Instance();
            try
            {
                string insightText = await insightWorkFlow.IngestRules(dataArray);

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
