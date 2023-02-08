//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class DeleteModelRequestParams : ModelRequestBase
    {
        /// <summary>
        /// Model id
        /// </summary>
        public int ModelId { get; set; }
    }

    /// <summary>
    /// Response class for delete model
    /// </summary>
    public class DeleteModelResponseParams : ModelResponseBase
    {
    }

    /// <summary>
    /// Request class to delete a model
    /// </summary>
    public class DeleteModelRequest
    {
        public static readonly
            RequestType<DeleteModelRequestParams, DeleteModelResponseParams> Type =
                RequestType<DeleteModelRequestParams, DeleteModelResponseParams>.Create("models/delete");
    }
}
