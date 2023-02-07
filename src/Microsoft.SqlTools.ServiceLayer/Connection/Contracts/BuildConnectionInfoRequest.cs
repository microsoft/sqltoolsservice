//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Serialize Connection String request 
    /// </summary>
    public class BuildConnectionInfoRequest
    {
        public static readonly
            RequestType<string, ConnectionDetails> Type =
            RequestType<string, ConnectionDetails>.Create("connection/buildconnectioninfo");
    }
}
