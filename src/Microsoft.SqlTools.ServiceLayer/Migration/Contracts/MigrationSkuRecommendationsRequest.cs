//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class MigrationSkuRecommendationsParams 
    {
        public string OwnerUri { get; set; }
    }

    public class MigrationSkuRecommendationsResult
    {
        public List<SkuRecommendationInfo> SkuRecommendations { get; set; }
    }

    /// <summary>
    /// Retreive metadata for the table described in the TableMetadataParams value
    /// </summary>
    public class MigrationSkuRecommendationsRequest
    {
        public static readonly
            RequestType<MigrationSkuRecommendationsParams, MigrationSkuRecommendationsResult> Type =
                RequestType<MigrationSkuRecommendationsParams, MigrationSkuRecommendationsResult>.Create("migration/getskurecommendations");
    }
}
