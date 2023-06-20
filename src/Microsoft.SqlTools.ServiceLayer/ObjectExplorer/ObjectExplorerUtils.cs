//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Contracts;
using System.Collections.Generic;

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
        /// <returns>A Tree Node that matches the condition, or null if no matching node could be found</returns>
        public static TreeNode? FindNode(TreeNode node, Predicate<TreeNode> condition, Predicate<TreeNode> filter, bool expandIfNeeded = false)
        {
            if (node == null)
            {
                return null;
            }

            if (condition(node))
            {
                return node;
            }
            var children = expandIfNeeded && !node.IsAlwaysLeaf ? node.Expand(new CancellationToken()) : node.GetChildren();
            foreach (var child in children)
            {
                if (filter != null && filter(child))
                {
                    TreeNode? childNode = FindNode(child, condition, filter, expandIfNeeded);
                    if (childNode != null)
                    {
                        return childNode;
                    }
                }
            }
            return null;
        }

        public static INodeFilter ConvertExpandNodeFilterToNodeFilter(NodeFilter filter, NodeFilterProperty filterProperty)
        {
            Type type = typeof(string);

            var IsDateTime = filterProperty.Type == NodeFilterPropertyDataType.Date;

            FilterType filterType = FilterType.EQUALS;
            bool isNotFilter = false;

            object filterValue = null;

            switch (filterProperty.Type)
            {
                case NodeFilterPropertyDataType.String:
                case NodeFilterPropertyDataType.Date:
                case NodeFilterPropertyDataType.Choice:
                    type = typeof(string);
                    filterValue = filter.Value.ToString();
                    break;
                case NodeFilterPropertyDataType.Number:
                    type = typeof(int);
                    filterValue = filter.Value.ToObject<int>();
                    break;
                case NodeFilterPropertyDataType.Boolean:
                    type = typeof(bool);
                    filterValue = filter.Value.ToObject<bool>() ? 1 : 0;
                    break;
            }

            switch (filter.Operator)
            {
                case NodeFilterOperator.Equals:
                    filterType = FilterType.EQUALS;
                    break;
                case NodeFilterOperator.NotEquals:
                    filterType = FilterType.EQUALS;
                    isNotFilter = true;
                    break;
                case NodeFilterOperator.LessThan:
                    filterType = FilterType.LESSTHAN;
                    break;
                case NodeFilterOperator.LessThanOrEquals:
                    filterType = FilterType.LESSTHANOREQUAL;
                    break;
                case NodeFilterOperator.GreaterThan:
                    filterType = FilterType.GREATERTHAN;
                    break;
                case NodeFilterOperator.GreaterThanOrEquals:
                    filterType = FilterType.GREATERTHANOREQUAL;
                    break;
                case NodeFilterOperator.Between:
                    filterType = FilterType.BETWEEN;
                    break;
                case NodeFilterOperator.NotBetween:
                    filterType = FilterType.NOTBETWEEN;
                    isNotFilter = true;
                    break;
                case NodeFilterOperator.Like:
                    filterType = FilterType.LIKE;
                    break;
                case NodeFilterOperator.NotLike:
                    filterType = FilterType.LIKE;
                    isNotFilter = true;
                    break;
            }


            if (filter.Operator == NodeFilterOperator.Between || filter.Operator == NodeFilterOperator.NotBetween)
            {
                if (filterProperty.Type == NodeFilterPropertyDataType.Number)
                {
                    filterValue = filter.Value.ToObject<int[]>();
                }
                else if (filterProperty.Type == NodeFilterPropertyDataType.Date)
                {
                    filterValue = filter.Value.ToObject<string[]>();
                }
            }

            return new NodePropertyFilter
            {
                Property = filterProperty.Name,
                Type = type,
                Values = new List<object> { filterValue },
                IsNotFilter = isNotFilter,
                FilterType = filterType,
                IsDateTime = IsDateTime
            };
        }
    }
}
