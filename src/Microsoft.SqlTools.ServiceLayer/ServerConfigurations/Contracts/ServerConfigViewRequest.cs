//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts
{
    public class ServerConfigViewRequestParams
    {
        /// <summary>
        /// Connection uri
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Config Number
        /// </summary>
        public int ConfigNumber { get; set; }
    }

    /// <summary>
    /// Response class for config view
    /// </summary>
    public class ServerConfigViewResponseParams
    {
        /// <summary>
        /// Config Property
        /// </summary>
        public ServerConfigProperty ConfigProperty { get; set; }
    }

    /// <summary>
    /// Request class for config view
    /// </summary>
    public class ServerConfigViewRequest
    {
        public static readonly
            RequestType<ServerConfigViewRequestParams, ServerConfigViewResponseParams> Type =
                RequestType<ServerConfigViewRequestParams, ServerConfigViewResponseParams>.Create("config/view");
    }
}
