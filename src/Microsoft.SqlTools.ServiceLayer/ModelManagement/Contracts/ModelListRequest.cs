//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ModelListRequestParams : ModelRequestBaseParams
    {
    }

    /// <summary>
    /// Response class for external language list
    /// </summary>
    public class ModelListResponseParams
    {
        /// <summary>
        /// Language status
        /// </summary>
        public List<RegisteredModel> Models { get; set; }
    }

    /// <summary>
    /// Request class for external language list
    /// </summary>
    public class ModelListRequest
    {
        public static readonly
            RequestType<ModelListRequestParams, ModelListResponseParams> Type =
                RequestType<ModelListRequestParams, ModelListResponseParams>.Create("modelManagement/list");
    }
}
