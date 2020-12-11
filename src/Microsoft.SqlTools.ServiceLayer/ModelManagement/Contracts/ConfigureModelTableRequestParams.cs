//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts
{
    public class ConfigureModelTableRequestParams : ModelRequestBase
    {
    }

    /// <summary>
    /// Response class for get model
    /// </summary>
    public class ConfigureModelTableResponseParams : ModelResponseBase
    {
    }

    /// <summary>
    /// Request class to get models
    /// </summary>
    public class ConfigureModelTableRequest
    {
        public static readonly
            RequestType<ConfigureModelTableRequestParams, ConfigureModelTableResponseParams> Type =
                RequestType<ConfigureModelTableRequestParams, ConfigureModelTableResponseParams>.Create("models/configure");
    }
}
