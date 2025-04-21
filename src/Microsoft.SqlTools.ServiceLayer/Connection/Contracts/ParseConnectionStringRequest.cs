//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parse Connection String request
    /// </summary>
    internal class ParseConnectionStringRequest
    {
        public static readonly
            RequestType<string, ConnectionDetails> Type =
            RequestType<string, ConnectionDetails>.Create("connection/parseConnectionString");
    }
}
