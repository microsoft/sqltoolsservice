//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the query cancellation request
    /// </summary>
    public class QueryRenameParams
    {
        public string NewOwnerUri { get; set; }
        public string OriginalOwnerUri { get; set; }
    }
    public class QueryRenameNotification
    {
        public static readonly 
            EventType<QueryRenameParams> Type =
            EventType<QueryRenameParams>.Create("query/rename");
    }
}
