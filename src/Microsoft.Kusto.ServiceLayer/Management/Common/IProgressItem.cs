//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
#region interfaces
    /// <summary>
    /// Interface that supports the delegation of individual actions in the progress dialog
    /// to individual classes.
    /// </summary>
    public interface IProgressItem
    {
        /// <summary>
        /// Perform the action for this class
        /// </summary>
        /// <param name="actions">Actions collection</param>
        /// <param name="index">array index of this particular action</param>
        /// <returns></returns>
        ProgressStatus DoAction(ProgressItemCollection actions, int index);
    }
#endregion
}
