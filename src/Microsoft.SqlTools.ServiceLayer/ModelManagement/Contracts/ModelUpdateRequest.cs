//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ModelUpdateRequestParams : ModelRequestBaseParams
    {
        /// <summary>
        /// Language name
        /// </summary>
        public RegisteredModel Model { get; set; }
    }

    /// <summary>
    /// Response class for external language update
    /// </summary>
    public class ModelUpdateResponseParams
    {
    }

    /// <summary>
    /// Request class for external language status
    /// </summary>
    public class ModelUpdateRequest
    {
        public static readonly
            RequestType<ModelUpdateRequestParams, ModelUpdateResponseParams> Type =
                RequestType<ModelUpdateRequestParams, ModelUpdateResponseParams>.Create("modelManagement/update");
    }
}
