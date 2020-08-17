//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Serialize Connection String request 
    /// </summary>
    public class BuildConnectionInfoRequest
    {
        public static readonly
            RequestType<string, ConnectionDetails> Type =
            RequestType<string, ConnectionDetails>.Create("kusto/connection/buildconnectioninfo");
    }
}
