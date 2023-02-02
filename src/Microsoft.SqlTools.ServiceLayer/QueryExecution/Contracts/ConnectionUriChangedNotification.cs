//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts
{
    /// <summary>
    /// Parameters for the connection uri changed notification.
    /// </summary>
    public class ConnectionUriChangedParams
    {
        public string NewOwnerUri { get; set; }
        public string OriginalOwnerUri { get; set; }
    }
    public class ConnectionUriChangedNotification
    {
        public static readonly 
            EventType<ConnectionUriChangedParams> Type =
            EventType<ConnectionUriChangedParams>.Create("query/connectionUriChanged");
    }
}
