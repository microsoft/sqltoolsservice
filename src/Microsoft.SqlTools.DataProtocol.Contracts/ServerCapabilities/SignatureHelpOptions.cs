//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Defines options for signature help that the server supports
    /// </summary>
    public class SignatureHelpOptions
    {
        /// <summary>
        /// Characters that trigger signature help automatically
        /// </summary>
        public string[] TriggerCharacters { get; set; }
    }
}