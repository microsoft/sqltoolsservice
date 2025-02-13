//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts
{
    public class GetSessionIdResponse
    {
        /// <summary>
        /// Unique ID to use when sending any requests for objects in the
        /// tree under the node
        /// </summary>
        public string SessionId { get; set; }
    }

    public class GetSessionIdRequest
    {
        public static readonly
            RequestType<ConnectionDetails, GetSessionIdResponse> Type =
            RequestType<ConnectionDetails, GetSessionIdResponse>.Create("objectexplorer/getsessionid");
    }
}
