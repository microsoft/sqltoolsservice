//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.MachineLearningServices.Contracts
{
    public class ExternalScriptConfigUpdateRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Config status value
        /// </summary>
        public bool Status { get; set; }
    }

    /// <summary>
    /// Response class for external script update 
    /// </summary>
    public class ExternalScriptConfigUpdateResponseParams
    {
        /// <summary>
        /// Config update result
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Error message is update fails
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Request class for external script update
    /// </summary>
    public class ExternalScriptConfigUpdateRequest
    {
        public static readonly
            RequestType<ExternalScriptConfigUpdateRequestParams, ExternalScriptConfigUpdateResponseParams> Type =
                RequestType<ExternalScriptConfigUpdateRequestParams, ExternalScriptConfigUpdateResponseParams>.Create("mls/externalscriptconfigupdate");
    }
}
