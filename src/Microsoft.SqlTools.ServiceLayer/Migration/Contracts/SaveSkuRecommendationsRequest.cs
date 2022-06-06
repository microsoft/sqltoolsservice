//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models.Sql;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class SaveSkuRecommendationsParams
    {
        /// <summary>
        /// List of SQL DB recommendation results, if applicable
        /// </summary>
        public List<SkuRecommendationResult> SqlDbRecommendationResults { get; set; }

        /// <summary>
        /// List of SQL MI recommendation results, if applicable
        /// </summary>
        public List<SkuRecommendationResult> SqlMiRecommendationResults { get; set; }

        /// <summary>
        /// List of SQL VM recommendation results, if applicable
        /// </summary>
        public List<SkuRecommendationResult> SqlVmRecommendationResults { get; set; }

        /// <summary>
        /// SQL instance requirements, representing an aggregated view of the performance requirements of the source instance
        /// </summary>
        public SqlInstanceRequirements InstanceRequirements { get; set; }
    }

    public class SaveSkuRecommendationsResult
    {
        /// <summary>
        /// Full file names where SKU Recommendation result is saved
        /// </summary>
        public List<string> SkuRecommendationsReportFileNames { get; set; }

    }

    public class SaveSkuRecommendationsResultRequest
    {
        public static readonly
            RequestType<SaveSkuRecommendationsParams, SaveSkuRecommendationsResult> Type =
                RequestType<SaveSkuRecommendationsParams, SaveSkuRecommendationsResult>.Create("migration/saveskurecommendations");
    }
}
