//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Abstract class for parameters that require an OwnerUri
    /// </summary>
    public abstract class SessionOperationParams
    {
        /// <summary>
        /// Owner URI for the session to add new row to
        /// </summary>
        public string OwnerUri { get; set; }
    }
}
