//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// A <see cref="ChildFactory"/> supports creation of <see cref="TreeNode"/> children
    /// for a class of objects in the tree. The 
    /// </summary>
    public abstract class ChildFactory
    {
        /// <summary>
        /// The set of applicable parents for which the factory can create children.
        /// </summary>
        /// <returns>
        /// the string names for each <see cref="TreeNode.NodeType"/> that 
        /// this factory can create children for
        /// </returns>
        public abstract IEnumerable<string> ApplicableParents();

        /// <summary>
        /// Expands an element in the 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public abstract IEnumerable<TreeNode> Expand(TreeNode parent);

        public abstract bool CanCreateChild(TreeNode parent, object context);
        public abstract TreeNode CreateChild(TreeNode parent, object context);

        // TODO Consider whether Remove operations need to be supported
        //public abstract bool CanRemoveChild(TreeNode parent, object context);
        //public abstract int GetChildIndexToRemove(TreeNode parent, object context);
    }
}
