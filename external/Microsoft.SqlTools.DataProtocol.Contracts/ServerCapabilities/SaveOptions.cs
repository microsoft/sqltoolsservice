//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Options the server supports for saving
    /// </summary>
    public class SaveOptions
    {
        /// <summary>
        /// Whether the client is supposed to include the content on save
        /// </summary>
        public bool? IncludeText { get; set; }
    }
}