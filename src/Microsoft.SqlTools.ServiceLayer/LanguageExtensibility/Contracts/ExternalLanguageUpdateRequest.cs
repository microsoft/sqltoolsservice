//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts
{
    public class ExternalLanguageUpdateRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Language name
        /// </summary>
        public ExternalLanguage Language { get; set; }
    }

    /// <summary>
    /// Response class for external language status
    /// </summary>
    public class ExternalLanguageUpdateResponseParams
    {
    }

    /// <summary>
    /// Request class for external language status
    /// </summary>
    public class ExternalLanguageUpdateRequest
    {
        public static readonly
            RequestType<ExternalLanguageUpdateRequestParams, ExternalLanguageUpdateResponseParams> Type =
                RequestType<ExternalLanguageUpdateRequestParams, ExternalLanguageUpdateResponseParams>.Create("languageExtension/update");
    }
}
