//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes
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
        /// The list of properties to be loaded with the object
        /// </summary>
        public abstract IEnumerable<NodeSmoProperty> SmoProperties { get; }

        // TODOKusto: Can this context be changed to DataSourceObjectMetadata

        // TODO Consider whether Remove operations need to be supported
        //public abstract bool CanRemoveChild(TreeNode parent, object context);
        //public abstract int GetChildIndexToRemove(TreeNode parent, object context);
    }
}
