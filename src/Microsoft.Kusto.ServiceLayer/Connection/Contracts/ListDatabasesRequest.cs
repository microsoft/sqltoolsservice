//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// List databases request mapping entry 
    /// </summary>
    public class ListDatabasesRequest
    {
        public static readonly
            RequestType<ListDatabasesParams, ListDatabasesResponse> Type =
            RequestType<ListDatabasesParams, ListDatabasesResponse>.Create("kusto/connection/listdatabases");
    }
}
