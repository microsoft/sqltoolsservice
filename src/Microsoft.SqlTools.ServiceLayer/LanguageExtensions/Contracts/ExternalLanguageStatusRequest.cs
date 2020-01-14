//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensions.Contracts
{
    public class ExternalLanguageStatusRequestParams
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
    public class ExternalLanguageStatusResponseParams
    {
        /// <summary>
        /// Language status
        /// </summary>
        public bool Status { get; set; }
    }

    /// <summary>
    /// Request class for external language status
    /// </summary>
    public class ExternalLanguageStatusRequest
    {
        public static readonly
            RequestType<ExternalLanguageStatusRequestParams, ExternalLanguageStatusResponseParams> Type =
                RequestType<ExternalLanguageStatusRequestParams, ExternalLanguageStatusResponseParams>.Create("languageextension/status");
    }
}
