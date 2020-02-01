//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts
{
    public class ExternalLanguageListRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Response class for external language list
    /// </summary>
    public class ExternalLanguageListResponseParams
    {
        /// <summary>
        /// Language status
        /// </summary>
        public List<ExternalLanguage> Languages { get; set; }
    }

    /// <summary>
    /// Request class for external language list
    /// </summary>
    public class ExternalLanguageListRequest
    {
        public static readonly
            RequestType<ExternalLanguageListRequestParams, ExternalLanguageListResponseParams> Type =
                RequestType<ExternalLanguageListRequestParams, ExternalLanguageListResponseParams>.Create("languageExtension/list");
    }
}
