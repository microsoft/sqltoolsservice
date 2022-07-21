//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlServer.Migration.SkuRecommendation.Models.Sql;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Migration.Contracts
{
    public class GenerateProvisioningScriptParams
    {
        /// <summary>
        /// List of SKU recommendations used to generate provisioning script
        /// </summary>
        public List<SkuRecommendationResult> recs { get; set; }
    }

    public class GenerateProvisioningScriptResult
    {
        /// <summary>
        /// String containing the provisioning script
        /// </summary>
        public string provisioningScript { get; set; }
    }

    public class GenerateProvisioningScriptRequest
    {
        public static readonly
            RequestType<GenerateProvisioningScriptParams, GenerateProvisioningScriptResult> Type =
                RequestType<GenerateProvisioningScriptParams, GenerateProvisioningScriptResult>.Create("migration/generateprovisioningscript");
    }
}