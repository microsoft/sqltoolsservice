//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class FeaturesMetadataProviderHelper
    {
        public static FeatureMetadataProvider[] CreateFeatureMetadataProviders()
        {
            List<FeatureMetadataProvider> features = new List<FeatureMetadataProvider>();

            features.Add(new FeatureMetadataProvider
            {
                FeatureName = "Restore",
                Enabled = true,
                OptionsMetadata = RestoreOptionsHelper.CreateRestoreOptions()
            });

            features.Add(new FeatureMetadataProvider
            {
                FeatureName = "serializationService",
                Enabled = true,
                OptionsMetadata = new ServiceOption[0]
            });

            return features.ToArray();
        }
    }
}
