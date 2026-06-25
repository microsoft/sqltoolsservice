//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{
    /// <summary>
    /// Parameters for the Parse Connection String Request.
    /// </summary>
    public class ParseConnectionStringParams
    {
        /// <summary>
        /// Connection string to parse into connection details.
        /// </summary>
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Parse Connection String request
    /// </summary>
    internal class ParseConnectionStringRequest
    {
        public static readonly
            RequestType<ParseConnectionStringParams, ConnectionDetails> Type =
            RequestType<ParseConnectionStringParams, ConnectionDetails>.Create("connection/parseConnectionString");
    }
}
