//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.InsightsGenerator.Contracts
{
    /// <summary>
    /// Query insights generator parameters
    /// </summary>
    public class QueryInsightsGeneratorParams : GeneralRequestDetails
    {
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Query insights generator result
    /// </summary>
    public class InsightsGeneratorResult : ResultStatus
    {
        public string[] InsightsText { get; set; }
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
