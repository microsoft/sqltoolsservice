//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Represents a folder node in the tree
    /// </summary>
    public class FolderNode : SmoTreeNode
    {
        /// <summary>
        /// For folders, this copies the context of its parent if available
        /// </summary>
        /// <returns></returns>
        public override object GetContext()
        {
            return Parent?.GetContext();
        }

        /// <summary>
        /// For folders, searches for its parent's SMO object rather than copying for itself
        /// </summary>
        /// <returns><see cref="NamedSmoObject"/> from this parent's parent, or null if not found</returns>
        public override NamedSmoObject GetParentSmoObject()
        {
            return ParentAs<SmoTreeNode>()?.GetParentSmoObject();
        }
    }
}
