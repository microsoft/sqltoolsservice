//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class UpdateModelRequestParams : ImportModelRequestParams
    {
    }

    /// <summary>
    /// Response class for import model
    /// </summary>
    public class UpdateModelResponseParams : ModelResponseBase
    {
    }

    /// <summary>
    /// Request class to import a model
    /// </summary>
    public class UpdateModelRequest
    {
        public static readonly
            RequestType<UpdateModelRequestParams, UpdateModelResponseParams> Type =
                RequestType<UpdateModelRequestParams, UpdateModelResponseParams>.Create("models/update");
    }
}
