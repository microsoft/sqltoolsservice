//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class SkuRecommendationInfo 
    {
        public string SkuName { get; set; }

        public CheckInfo[] AssessmentResults { get; set; }
    }
}
