//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class GetSkuRecommendationsParams 
    {
        /// <summary>
        /// Folder from which collected performance data will be read from
        /// </summary>
        public string DataFolder { get; set; }

        /// <summary>
        /// Interval at which collected performance data was originally queried at, in seconds
        /// </summary>
        public int PerfQueryIntervalInSec { get; set; }

        /// <summary>
        /// List of target platforms to consider when generating recommendations
        /// </summary>
        public List<string> TargetPlatforms { get; set; }

        /// <summary>
        /// Name of the SQL instance to generate recommendations for
        /// </summary>
        public string TargetSqlInstance { get; set; }

        /// <summary>
        /// Target percentile to use when performing perf data aggregation
        /// </summary>
        public int TargetPercentile { get; set; }

        /// <summary>
        /// Scaling ("comfort") factor when evalulating performance requirements
        /// </summary>
        public int ScalingFactor { get; set; }

        /// <summary>
        /// Start time of collected data points to consider 
        /// 
        /// TO-DO: do we really need this? it's pretty safe to assume that most users would want us to evaluate all the collected data and not just part of it
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// End time of collected data points to consider
        /// 
        /// TO-DO: do we really need this? it's pretty safe to assume that most users would want us to evaluate all the collected data and not just part of it
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// Whether or not to use the elastic recommendation model (as opposed to the baseline model)
        /// 
        /// TO-DO: remove this - for now we're going to run both models but only display the results from the baseline model, but still use the gathered data to train the elastic model
        /// </summary>
        public bool ElasticStrategy { get; set; }

        /// <summary>
        /// List of databases to consider when generating recommendations
        /// </summary>
        public List<string> DatabaseAllowList { get; set; }
    }

    public class GetSkuRecommendationsResult
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
    }

    public class GetSkuRecommendationsRequest
    {
        public static readonly
            RequestType<GetSkuRecommendationsParams, GetSkuRecommendationsResult> Type =
                RequestType<GetSkuRecommendationsParams, GetSkuRecommendationsResult>.Create("migration/getskurecommendations");
    }
}
