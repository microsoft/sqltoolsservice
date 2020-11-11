//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts
{
    /// <summary>
    /// Language metadata
    /// </summary>
    public class ExternalLibraryModel
    {
        public string LanguageName { get; set; }

        /// <summary>
        /// Language Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Language Owner
        /// </summary>
        public string Owner { get; set; }

        public string FilePath { get; set; }
    }
}
