//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// List tables request mapping entry 
    /// </summary>
    public class ListTablesRequest
    {
        public static readonly
            RequestType<ListTablesParams, ListTablesResponse> Type =
            RequestType<ListTablesParams, ListTablesResponse>.Create("connection/listtables");
    }
}
