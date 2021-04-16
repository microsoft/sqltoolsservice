//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Options the server supports regarding command execution
    /// </summary>
    public class ExecuteCommandOptions
    {
        /// <summary>
        /// Commands the server can execute
        /// </summary>
        public string[] Commands { get; set; }
    }
}