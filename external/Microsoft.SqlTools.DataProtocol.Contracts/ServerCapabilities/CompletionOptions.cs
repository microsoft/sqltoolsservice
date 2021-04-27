//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Defines completion options that the server can support
    /// </summary>
    public class CompletionOptions
    {
        /// <summary>
        /// Most tools trigger completion request automatically without explicitly requesting it
        /// using a keyboard shortcut (eg, ctrl+space). Typically they do so when the user starts
        /// to type an identifier. For example if the user types 'c' in a JavaScript file, code
        /// completion will automatically pop up and present 'console' besides others as a
        /// completion item. Characters that make up identifiers don't need to be listed here.
        /// 
        /// If code completion should automatically be triggered on characters not being valid
        /// inside an identifier (for example '.' in JavaScript) list them in this.
        /// </summary>
        public string[] TriggerCharacters { get; set; }
        
        /// <summary>
        /// Server provides support to resolve additional information about a completion item
        /// </summary>
        public bool? ResolveProvider { get; set; }
    }
}