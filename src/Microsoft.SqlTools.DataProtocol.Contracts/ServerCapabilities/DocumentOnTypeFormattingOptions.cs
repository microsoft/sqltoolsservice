//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Options the server supports regarding formatting a document on type
    /// </summary>
    public class DocumentOnTypeFormattingOptions
    {
        /// <summary>
        /// Character on which formatting should be triggered, eg '}'
        /// </summary>
        public string FirstTriggerCharacter { get; set; }
        
        /// <summary>
        /// More charactres that trigger formatting
        /// </summary>
        public string[] MoreTriggerCharacters { get; set; }
    }
}