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
// - `perfQueryIntervalInSec`: Optional. Interval at which performance data was queried, in seconds. **Note:** This must match the value that was originally used during the performance data collection. (Default: `30`)
// - `targetPlatform`: Optional. Target platform for SKU recommendation: either `AzureSqlDatabase`, `AzureSqlManagedInstance`, `AzureSqlVirtualMachine`, or `Any`. If `Any` is selected, then SKU recommendations for all three target platforms will be evaluated, and the best fit will be returned. (Default: `Any`)
// - `targetSqlInstance`: Optional. Name of the SQL instance that SKU recommendation will be targeting. (Default: `outputFolder` will be scanned for files created by the `PerfDataCollection` action, and recommendations will be provided for every instance found) 
// - `targetPercentile`: Optional. Percentile of data points to be used during aggregation of the performance data. Only used for baseline (non-elastic) strategy. (Default: `95`)
// - `scalingFactor`: Optional. Scaling ('comfort') factor used during SKU recommendation. For example, if it is determined that there is a 4 vCore CPU requirement with a scaling factor of 150%, then the true CPU requirement will be 6 vCores. (Default: `100`)
// - `startTime`: Optional. UTC start time of performance data points to consider during aggregation, in `"YYYY-MM-DD HH:MM"` format. Only used for baseline (non-elastic) strategy. (Default: all data points collected will be considered)
// - `endTime`: Optional. UTC end time of performance data points to consider during aggregation, in `"YYYY-MM-DD HH:MM"` format. Only used for baseline (non-elastic) strategy. (Default: all data points collected will be considered)
// - `overwrite`: Optional. Whether or not to overwrite any existing SKU recommendation reports. (Default: `true`)
// - `displayResult`: Optional. Whether or not to print the SKU recommendation results to the console. (Default: `true`)
// - `elasticStrategy`: Optional. Whether or not to use the elastic strategy for SKU recommendations based on resource usage profiling. (Default: `false`)
// - `databaseAllowList`: Optional. Space separated list of names of databases to be allowed for SKU recommendation consideration while excluding all others. Only set one of the following or neither: databaseAllowList, databaseDenyList. (Default: `null`)
// - `databaseDenyList`: Optional. Space separated list of names of databases to not be considered for SKU recommendation. Only set one of the following or neither: databaseAllowList, databaseDenyList. (Default: `null`)


        public int PerfQueryIntervalInSec { get; set; }
        public string TargetPlatform { get; set; }
        public string TargetSqlInstance { get; set; }
        public int TargetPercentile { get; set; }
        public int ScalingFactor { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool ElasticStrategy { get; set; }

        public List<string> DatabasesAllowList { get; set; }

        public List<string> DatabaseDenyList { get; set; }
    }

    public class GetSkuRecommendationsResult
    {
        /// <summary>
        /// List of SKU recommendation results returned by recommendation engine in NuGet
        /// </summary>
        public List<SkuRecommendationResult> SqlDbRecommendationResults { get; set; }
        public List<SkuRecommendationResult> SqlMiRecommendationResults { get; set; }

        public List<SkuRecommendationResult> SqlVmRecommendationResults { get; set; }
    }

    public class GetSkuRecommendationsRequest
    {
        public static readonly
            RequestType<GetSkuRecommendationsParams, GetSkuRecommendationsResult> Type =
                RequestType<GetSkuRecommendationsParams, GetSkuRecommendationsResult>.Create("migration/getskurecommendations");
    }
}
