//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// ConnectionComplete notification mapping entry 
    /// </summary>
    public class ConnectionServerlessRetryNotification
    {
        public static readonly 
            EventType<string> Type =
            EventType<string>.Create("connection/serverlessretry");
    }
}
