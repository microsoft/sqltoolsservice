//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// ConnectionChanged notification mapping entry 
    /// </summary>
    public class ConnectionChangedNotification
    {
        public static readonly
            EventType<ConnectionChangedParams> Type =
            EventType<ConnectionChangedParams>.Create("kusto/connection/connectionchanged");
    }
}
