//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.Hosting.Hosting.Contracts
{
    /// <summary>
    /// Includes the metadata for a feature
    /// </summary>
    public class FeatureMetadataProvider
    {
        /// <summary>
        /// Indicates whether the feature is enabled 
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Feature name
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// The options metadata avaialble for this feature
        /// </summary>
        public ServiceOption[] OptionsMetadata { get; set; }

    }
}
