//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel;

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
        /// <param name="parent">Parent Node</param>
        /// <param name="refresh">force to refresh</param>
        /// <param name="refresh">name of the sql object to filter</param>
        /// <returns></returns>
        public abstract IEnumerable<TreeNode> Expand(TreeNode parent, bool refresh, string name, bool includeSystemObjects);

        /// <summary>
        /// The list of filters that should be applied on the smo object list
        /// </summary>
        public abstract IEnumerable<NodeFilter> Filters { get; }

        /// <summary>
        /// Returns the node sub type if the object can have sub types otehr wise returns empty string
        /// </summary>
        public abstract string GetNodeSubType(object context);

        /// <summary>
        /// Returns the status of the object assigned to node. If the object doesn't spport status returns empty string
        /// </summary>
        public abstract string GetNodeStatus(object context);

        /// <summary>
        /// Returns the custom name of the object assigned to the node. If the object doesn't have custom name, returns empty string
        /// </summary>
        public abstract string GetNodeCustomName(object smoObject, SmoQueryContext smoContext);

        public abstract bool CanCreateChild(TreeNode parent, object context);
        public abstract TreeNode CreateChild(TreeNode parent, object context);

        // TODO Consider whether Remove operations need to be supported
        //public abstract bool CanRemoveChild(TreeNode parent, object context);
        //public abstract int GetChildIndexToRemove(TreeNode parent, object context);
    }
}
