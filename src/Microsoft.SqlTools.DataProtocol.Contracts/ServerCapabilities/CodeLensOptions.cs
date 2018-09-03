//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.DataProtocol.Contracts.ServerCapabilities
{
    /// <summary>
    /// Options about Code Lens that the server supports
    /// </summary>
    public class CodeLensOptions
    {
        /// <summary>
        /// Code lens has a resolve provider, as well
        /// TODO: WTF does this mean??
        /// </summary>
        public bool? ResolveProvider { get; set; }
    }
}