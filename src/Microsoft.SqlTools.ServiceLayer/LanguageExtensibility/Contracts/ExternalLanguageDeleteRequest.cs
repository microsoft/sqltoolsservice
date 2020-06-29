//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts
{
    public class ExternalLanguageDeleteRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Language name
        /// </summary>
        public string LanguageName { get; set; }
    }

    /// <summary>
    /// Response class for external language status
    /// </summary>
    public class ExternalLanguageDeleteResponseParams
    {
    }

    /// <summary>
    /// Request class for external language status
    /// </summary>
    public class ExternalLanguageDeleteRequest
    {
        public static readonly
            RequestType<ExternalLanguageDeleteRequestParams, ExternalLanguageDeleteResponseParams> Type =
                RequestType<ExternalLanguageDeleteRequestParams, ExternalLanguageDeleteResponseParams>.Create("languageExtension/delete");
    }
}
