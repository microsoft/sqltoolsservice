//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.InsightsGenerator.Contracts
{
    public class AccessibleChartData 
    {
	    public string[] Columns { get; set; }
		public string[][] Rows { get; set; }
    }

    /// <summary>
    /// Query insights generator parameters
    /// </summary>
    public class QueryInsightsGeneratorParams : GeneralRequestDetails
    {
        public AccessibleChartData Data { get; set; }
    }

    /// <summary>
    /// Query insights generator result
    /// </summary>
    public class InsightsGeneratorResult : ResultStatus
    {
        public string InsightsText { get; set; }
    }

    /// <summary>
    /// Query insights generato request type
    /// </summary>
    public class QueryInsightsGeneratorRequest
    {
        /// <summary>
        /// Request definition
        /// </summary>
        public static readonly
            RequestType<QueryInsightsGeneratorParams, InsightsGeneratorResult> Type =
            RequestType<QueryInsightsGeneratorParams, InsightsGeneratorResult>.Create("insights/query");
    } 
}
