//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ModelDeleteRequestParams : ModelRequestBaseParams
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Response class for external language status
    /// </summary>
    public class ModelDeleteResponseParams
    {
    }

    /// <summary>
    /// Request class for external language status
    /// </summary>
    public class ModelDeleteRequest
    {
        public static readonly
            RequestType<ModelDeleteRequestParams, ModelDeleteResponseParams> Type =
                RequestType<ModelDeleteRequestParams, ModelDeleteResponseParams>.Create("modelManagement/delete");
    }
}
