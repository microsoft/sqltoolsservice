//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer
{
    /// <summary>
    /// Utility class for Object Explorer related operations
    /// </summary>
    public static class ObjectExplorerUtils
    {
        /// <summary>
        /// Visitor that walks all nodes from the child to the root node, unless the 
        /// <paramref name="visitor"/> function indicates that this should stop traversing
        /// </summary>
        /// <param name="child">node to start traversing at</param>
        /// <param name="visitor">Predicate function that accesses the tree and
        /// determines whether to stop going further up the tree</param>
        /// <returns>
        /// boolean - true to continue navigating up the tree, false to end the loop
        /// and return early
        /// </returns>
        public static bool VisitChildAndParents(TreeNode child, Predicate<TreeNode> visitor)
        {
            if (child == null)
            {
                // End case: all nodes have been visited
                return true;
            }

            // Visit the child first, then go up the parents
            if (!visitor(child))
            {
                return false;
            }
            return VisitChildAndParents(child.Parent, visitor);
        }

        /// <summary>
        /// Finds a node by traversing the tree starting from the given node through all the children
        /// </summary>
        /// <param name="node">node to start traversing at</param>
        /// <param name="condition">Predicate function that accesses the tree and
        /// determines whether to stop going further up the tree</param>
        /// <param name="filter">Predicate function to filter the children when traversing</param>
        /// <returns>A Tree Node that matches the condition</returns>
        public static TreeNode FindNode(TreeNode node, Predicate<TreeNode> condition, Predicate<TreeNode> filter)
        {
            if(node == null)
            {
                return null;
            }

            if (condition(node))
            {
                return node;
            }
            foreach (var child in node.GetChildren())
            {
                if (filter != null && filter(child))
                {
                    TreeNode childNode = FindNode(child, condition, filter);
                    if (childNode != null)
                    {
                        return childNode;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Check if the database is a system database
        /// </summary>
        /// <param name="databaseName">the name of database</param>
        /// <returns>return true if the database is a system database</returns>
        public static bool IsSystemDatabaseConnection(string databaseName)
        {
            return (string.IsNullOrWhiteSpace(databaseName) ||
                string.Compare(databaseName, CommonConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.MsdbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.ModelDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.TempDbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
