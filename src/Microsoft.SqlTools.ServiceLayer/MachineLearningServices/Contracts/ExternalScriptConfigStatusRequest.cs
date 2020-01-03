//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.MachineLearningServices.Contracts
{
    public class ExternalScriptConfigStatusRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }
    }

    /// <summary>
    /// Response class for external script config status
    /// </summary>
    public class ExternalScriptConfigStatusResponseParams
    {
        public bool Status { get; set; }
    }

    /// <summary>
    /// Request class extrenal script config status
    /// </summary>
    public class ExternalScriptConfigStatusRequest
    {
        public static readonly
            RequestType<ExternalScriptConfigStatusRequestParams, ExternalScriptConfigStatusResponseParams> Type =
                RequestType<ExternalScriptConfigStatusRequestParams, ExternalScriptConfigStatusResponseParams>.Create("mls/externalscriptconfigstatus");
    }
}
