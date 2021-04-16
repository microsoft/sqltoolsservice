//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Options the server support for document links
    /// </summary>
    public class DocumentLinkOptions
    {
        /// <summary>
        /// Document links have a resolve provider, as well
        /// TODO: WTF does this mean?
        /// </summary>
        public bool? ResolveProvider { get; set; }
    }
}