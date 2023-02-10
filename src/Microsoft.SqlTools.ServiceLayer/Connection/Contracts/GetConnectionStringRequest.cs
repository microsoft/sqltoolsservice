//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Get Connection String request
    /// </summary>
    public class GetConnectionStringRequest
    {
        public static readonly
            RequestType<GetConnectionStringParams, string> Type =
            RequestType<GetConnectionStringParams, string>.Create("connection/getconnectionstring");
    }
}
