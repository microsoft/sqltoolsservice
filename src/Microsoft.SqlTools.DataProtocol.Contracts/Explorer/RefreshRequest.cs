//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts.Explorer
{
    /// <summary>
    /// Parameters to the <see cref="ExpandRequest"/>.
    /// </summary>
    public class RefreshParams: ExpandParams
    {
    }

    /// <summary>
    /// A request to expand a 
    /// </summary>
    public class RefreshRequest
    {
        /// <summary>
        /// Returns children of a given node as a <see cref="NodeInfo"/> array.
        /// </summary>
        public static readonly
            RequestType<RefreshParams, bool> Type =
            RequestType<RefreshParams, bool>.Create("explorer/refresh");
    }
}
