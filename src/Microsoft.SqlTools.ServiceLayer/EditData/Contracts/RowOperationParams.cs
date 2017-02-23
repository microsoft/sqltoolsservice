//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.EditData.Contracts
{
    /// <summary>
    /// Abstract class for parameters that require an OwnerUri and a RowId
    /// </summary>
    public abstract class RowOperationParams : SessionOperationParams
    {
        /// <summary>
        /// Internal ID of the row to revert
        /// </summary>
        public long RowId { get; set; }
    }
}
