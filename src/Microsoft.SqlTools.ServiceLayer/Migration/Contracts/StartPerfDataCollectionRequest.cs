
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class StartPerfDataCollectionParams
    {
        public string OwnerUri { get; set; }
        public string DataFolder { get; set; }
        public int PerfQueryIntervalInSec { get; set; }
        public int StaticQueryIntervalInSec { get; set; }
        public int NumberOfIterations { get; set; }
    }

    //public class GetSkuRecommendationsResult
    //{
    //    /// <summary>
    //    /// List of SKU recommendation results returned by recommendation engine in NuGet
    //    /// </summary>
    //    public List<SkuRecommendationResult> SqlDbRecommendationResults { get; set; }
    //    public List<SkuRecommendationResult> SqlMiRecommendationResults { get; set; }

    //    public List<SkuRecommendationResult> SqlVmRecommendationResults { get; set; }
    //}

    public class StartPerfDataCollectionRequest
    {
        public static readonly
            RequestType<StartPerfDataCollectionParams, int> Type =
                RequestType<StartPerfDataCollectionParams, int>.Create("migration/startperfdatacollection");
    }
}
