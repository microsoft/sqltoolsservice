//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ServerConfigurations.Contracts
{
    public class ServerConfigListRequestParams
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
    /// Response class for config list
    /// </summary>
    public class ServerConfigListResponseParams
    {
        /// <summary>
        /// Config Property
        /// </summary>
        public List<ServerConfigProperty> ConfigProperties { get; set; }
    }

    /// <summary>
    /// Request class for config list
    /// </summary>
    public class ServerConfigListRequest
    {
        public static readonly
            RequestType<ServerConfigListRequestParams, ServerConfigListResponseParams> Type =
                RequestType<ServerConfigListRequestParams, ServerConfigListResponseParams>.Create("config/List");
    }
}
