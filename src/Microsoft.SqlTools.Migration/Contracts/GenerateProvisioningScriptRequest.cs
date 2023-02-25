//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlServer.Migration.SkuRecommendation.Contracts.Models;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class GenerateProvisioningScriptParams
    {
        /// <summary>
        /// List of SKU recommendations used to generate provisioning script
        /// </summary>
        public List<SkuRecommendationResult> SkuRecommendations { get; set; }

        /// <summary>
        /// Server level collation
        /// </summary>
        public string ServerLevelCollation { get; set; }

        /// <summary>
        /// Mapping of database names to database collation
        /// </summary>
        public List<DatabaseCollationMapping> DatabaseLevelCollations { get; set; }
    }

    public class GenerateProvisioningScriptResult
    {   
        /// <summary>
        /// String containing the filepath of the provisioning script
        /// </summary>
        public string ProvisioningScriptFilePath { get; set; }
    }

    public class GenerateProvisioningScriptRequest
    {
        public static readonly
            RequestType<GenerateProvisioningScriptParams, GenerateProvisioningScriptResult> Type =
                RequestType<GenerateProvisioningScriptParams, GenerateProvisioningScriptResult>.Create("migration/generateprovisioningscript");
    }

    public class DatabaseCollationMapping
    {
        public string DatabaseName;

        public string DatabaseCollation;
    }
}