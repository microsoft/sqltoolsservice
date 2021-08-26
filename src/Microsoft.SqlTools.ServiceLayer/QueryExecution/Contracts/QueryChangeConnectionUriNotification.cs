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
    public class QueryChangeUriParams
    {
        public string NewOwnerUri { get; set; }
        public string OriginalOwnerUri { get; set; }
    }
    public class QueryChangeUriNotification
    {
        public static readonly 
            EventType<QueryChangeUriParams> Type =
            EventType<QueryChangeUriParams>.Create("query/ChangeUri");
    }
}
