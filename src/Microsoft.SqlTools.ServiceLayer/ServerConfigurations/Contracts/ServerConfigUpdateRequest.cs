//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts
{
    public class ServerConfigUpdateRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Config number
        /// </summary>
        public int ConfigNumber { get; set; }

        /// <summary>
        /// Config value
        /// </summary>
        public int ConfigValue { get; set; }
    }

    /// <summary>
    /// Response class for config update
    /// </summary>
    public class ServerConfigUpdateResponseParams
    {
        /// <summary>
        /// Config Property
        /// </summary>
        public ServerConfigProperty ConfigProperty { get; set; }
    }

    /// <summary>
    /// Request class for config update
    /// </summary>
    public class ServerConfigUpdateRequest
    {
        public static readonly
            RequestType<ServerConfigUpdateRequestParams, ServerConfigUpdateResponseParams> Type =
                RequestType<ServerConfigUpdateRequestParams, ServerConfigUpdateResponseParams>.Create("config/update");
    }
}
