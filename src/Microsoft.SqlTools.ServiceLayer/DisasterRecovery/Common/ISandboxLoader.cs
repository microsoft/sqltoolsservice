//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Common
{
    /// <summary>
    /// To access DataModelingSandbox from NavigableItem
    /// </summary>
    public interface ISandboxLoader
    {
        /// <summary>
        /// Get sandbox
        /// </summary>
        /// <returns>DataModelingSandbox object associated with this NavigableItem</returns>
        object GetSandbox();

        /// <summary>
        /// Refresh sandbox data associated with this NavigableItem
        /// </summary>
        void RefreshSandboxData();

        /// <summary>
        /// Delete sandbox from cache
        /// </summary>
        void DeleteSandbox();
    }
}
