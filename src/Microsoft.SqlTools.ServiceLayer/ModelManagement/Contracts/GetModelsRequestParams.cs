//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class GetModelsRequestParams : ModelRequestBase
    {
    }

    /// <summary>
    /// Response class for get model
    /// </summary>
    public class GetModelsResponseParams : ModelResponseBase
    {
        public List<ModelMetadata> Models { get; set; }
    }

    /// <summary>
    /// Request class to get models
    /// </summary>
    public class GetModelsRequest
    {
        public static readonly
            RequestType<GetModelsRequestParams, GetModelsResponseParams> Type =
                RequestType<GetModelsRequestParams, GetModelsResponseParams>.Create("models/get");
    }
}
