//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Hosting.Contracts;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class FeaturesMetadataProviderHelper
    {
        public static FeatureMetadataProvider[] CreateFratureMetadataProviders()
        {
            List<FeatureMetadataProvider> featues = new List<FeatureMetadataProvider>();

            featues.Add(new FeatureMetadataProvider
            {
                FeatureName = "Restore",
                Enabled = true,
                OptionsMetadata = RestoreOprtionsHelper.CreateRestoreOptions()
            });

            return featues.ToArray();
        }
    }
}
