//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class VerifyModelTableRequestParams : ModelRequestBase
    {
    }

    /// <summary>
    /// Response class for verify model table
    /// </summary>
    public class VerifyModelTableResponseParams : ModelResponseBase
    {
        /// <summary>
        /// Specified is model table is verified
        /// </summary>
        public bool Verified { get; set; }
    }

    /// <summary>
    /// Request class to verify model table
    /// </summary>
    public class VerifyModelTableRequest
    {
        public static readonly
            RequestType<VerifyModelTableRequestParams, VerifyModelTableResponseParams> Type =
                RequestType<VerifyModelTableRequestParams, VerifyModelTableResponseParams>.Create("models/verify");
    }
}
