//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.ExecutionPlan.ExecPlanGraph.Comparison
{
    /// <summary>
    /// Handles operations for creating and comparing skeletons of showplan trees
    /// A skeleton is the tree with some nodes filtered out to reduce complexity
    /// </summary>
    public class SkeletonManager
    {
        public SkeletonManager() { }

        /// <summary>
        /// Constructs a skeleton tree representing logical structure of the showplan
        /// Primarily represents joins and data access operators
        /// </summary>
        /// <param name="root">Node to construct skeleton of</param>
        /// <returns>SkeletonNode with children representing logical descendants of the input node</returns>
        public SkeletonNode CreateSkeleton(Node root)
        {
            if (root == null)
                return null;

            Node rootNode = root;
            var childCount = root.Children.Count;
            if (childCount > 1)
            {
                SkeletonNode skeletonParent = new SkeletonNode(root);
                foreach (Node child in root.Children)
                {
                    SkeletonNode skeletonChild = CreateSkeleton(child);
                    skeletonParent.AddChild(skeletonChild);
                }
                return skeletonParent;
            }
            else if (childCount == 1)
            {
                if (!ShouldIgnoreDuringComparison(rootNode))
                {
                    // get children recursively then return this node to add it to the skeleton
                    SkeletonNode skeletonParent = new SkeletonNode(root);
                    SkeletonNode child = CreateSkeleton(root.Children.ElementAt(0));
                    skeletonParent.AddChild(child);
                    return skeletonParent;
                }
                // if ignoring root, just go on to the next node
                return CreateSkeleton(root.Children.First());
            }
            // no children; base case
            SkeletonNode skeletonNode = new SkeletonNode(root);
            return skeletonNode;
        }

        /// <summary>
        /// Checks root and all children for equivalent tree structure and logical equivalence at the node level
        /// </summary>
        /// <param name="root1"></param>
        /// <param name="root2"></param>
        /// <returns></returns>
        public bool AreSkeletonsEquivalent(SkeletonNode root1, SkeletonNode root2, bool ignoreDatabaseName)
        {
            if (root1 == null && root2 == null)
                return true;

            if (root1 == null || root2 == null)
                return false;

            if (!root1.BaseNode.IsLogicallyEquivalentTo(root2.BaseNode, ignoreDatabaseName))
            {
                return false;
            }
            if (root1.Children.Count != root2.Children.Count)
            {
                return false;
            }
            var childIterator = 0;
            while (childIterator < root1.Children.Count)
            {
                var checkMatch = AreSkeletonsEquivalent(root1.Children.ElementAt(childIterator), root2.Children.ElementAt(childIterator), ignoreDatabaseName);
                if (!checkMatch)
                {
                    // at least one pair of children (ie inner.Child1 & outer.Child1) didn't match; stop checking rest
                    return false;
                }
                childIterator++;
            }
            return true;
        }

        /// <summary>
        /// Finds the largest matching subtrees in two skeletons and colors those subtrees a unique color
        /// </summary>
        /// <param name="skeleton1"></param>
        /// <param name="skeleton2"></param>
        public void ColorMatchingSections(SkeletonNode skeleton1, SkeletonNode skeleton2, bool ignoreDatabaseName)
        {
            // starting node for the outer loop iteration
            SkeletonNode outerNode = skeleton1;
            Queue<SkeletonNode> outerQueue = new Queue<SkeletonNode>();

            int groupIndexCounter = 1;

            // Iterates over all nodes in skeleton1
            while (outerNode != null)
            {
                bool matchFound = false;
                SkeletonNode innerNode = skeleton2;
                Queue<SkeletonNode> innerQueue = new Queue<SkeletonNode>();
                // to find all the match sleleton2 node for skeleton1, iterate over each node of skeleton2 until all innerNode have been tested, there might be multiple match
                while (innerNode != null)
                {
                    if (this.AreSkeletonsEquivalent(outerNode, innerNode, ignoreDatabaseName))
                    {
                        matchFound = true;
                        int matchColor = groupIndexCounter++;
                        if (innerNode.ParentNode != null && innerNode.ParentNode.HasMatch)
                        {
                            int parentColor = innerNode.ParentNode.GroupIndex;
                            int innerColor = innerNode.GroupIndex;
                            // innerNode is the root of a matching subtree, so use its color for the new match instead of the random new color
                            if (parentColor != innerColor)
                            {
                                matchColor = innerColor;
                            }
                        }
                        else if (innerNode.HasMatch)
                        {
                            matchColor = innerNode.GroupIndex;
                        }
                        else if (outerNode.HasMatch)
                        {
                            // outerNode already finds a matching innerNode, but we keep looking for more matching innerNode
                            matchColor = outerNode.GroupIndex;
                        }

                        outerNode.AddMatchingSkeletonNode(innerNode, ignoreDatabaseName);
                        innerNode.AddMatchingSkeletonNode(outerNode, ignoreDatabaseName);
                        outerNode.ChangeSkeletonGroupIndex(matchColor);
                        innerNode.ChangeSkeletonGroupIndex(matchColor);
                    }

                    // even if we found a matching innerNode, we keep looking since there might be other innerNode that matches same outerNode
                    foreach (SkeletonNode child in innerNode.Children)
                    {
                        innerQueue.Enqueue(child);
                    }
                    innerNode = innerQueue.Any() ? innerQueue.Dequeue() : null;

                }

                // no match at all, so add this node's children to queue of nodes to check
                // effectively does a bfs - doesn't check children if a match has been found (and the entire subtree colored)
                if (!matchFound)
                {
                    foreach (SkeletonNode child in outerNode.Children)
                    {
                        outerQueue.Enqueue(child);
                    }
                }
                outerNode = outerQueue.Any() ? outerQueue.Dequeue() : null;
            }
        }

        public Node FindNextNonIgnoreNode(Node node)
        {
            Node curNode = node;
            while (curNode != null && ShouldIgnoreDuringComparison(curNode))
            {
                if (curNode.Children.Count > 0)
                {
                    curNode = curNode.Children.ElementAt(0);
                }
                else
                {
                    // should ignore, but this is a leaf node, so there is no matching node
                    curNode = null;
                }
            }
            return curNode;
        }

        #region Private Methods

        /// <summary>
        /// Determines if the node should be ignored when building a skeleton of the showplan
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ShouldIgnoreDuringComparison(Node node)
        {
            return IgnoreWhenBuildingSkeleton.Contains(node.Operation.Name) || (node[NodeBuilderConstants.LogicalOp] == null);
        }

        #endregion

        /// <summary>
        /// List of Node names which can safely be ignored when building the skeleton
        /// We can ignore these because not finding a matching between them shouldn't imapct of the shaping of matching nodes
        /// However, if in the future we see a use case to benefit from matching one of them, for ex I removed Filter because we need it to be skeleton node
        /// so we user can find issue for, and jump to the Filter node pair when doing scenario based issue detection
        /// </summary>
        private List<string> IgnoreWhenBuildingSkeleton = new List<string> { SR.Keys.Assert, SR.Keys.BatchHashTableBuild, SR.Keys.Bitmap, SR.Keys.Collapse, SR.Keys.RepartitionStreams,
            SR.Keys.ComputeScalar, SR.Keys.MergeInterval, SR.Keys.Parallelism, SR.Keys.Print, SR.Keys.RowCountSpool, SR.Keys.LogicalOpLazySpool,
            SR.Keys.TableSpool, SR.Keys.Segment, SR.Keys.SequenceProject, SR.Keys.Split, SR.Keys.Spool, SR.Keys.Window,
            SR.Keys.Sort, SR.Keys.Top, SR.Keys.LogicalOpTopNSort };
    }
}
