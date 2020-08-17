//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Get Connection String request
    /// </summary>
    public class GetConnectionStringRequest
    {
        public static readonly
            RequestType<GetConnectionStringParams, string> Type =
            RequestType<GetConnectionStringParams, string>.Create("kusto/connection/getconnectionstring");
    }
}
