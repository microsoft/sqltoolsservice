//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.ObjectExplorer.SmoModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes
{
    /// <summary>
    /// A collection class for <see cref="TreeNode"/>
    /// </summary>
    public sealed class NodeObservableCollection : ObservableCollection<TreeNode>
    {
        public event EventHandler Initialized;
        private int? numInits;
        private static int cleanupBlocker;

        public bool IsInitialized
        {
            get { return numInits.HasValue && numInits == 0; }
        }

        public bool IsPopulating
        {
            get { return numInits.HasValue && numInits != 0; }
        }

        public bool IsSorted
        {
            get
            {
                // SMO objects are already sorted so no need to sort them again
                return this.FirstOrDefault() is SmoTreeNode;
            }
        }

        public void BeginInit()
        {
            if (!numInits.HasValue)
            {
                numInits = 1;
            }
            else
            {
                numInits = numInits + 1;
            }
        }

        public void EndInit()
        {
            IList<TreeNode> empty = null;
            EndInit(null, ref empty);
        }

        public void EndInit(TreeNode parent, ref IList<TreeNode> deferredChildren)
        {
            if (numInits.HasValue &&
                numInits.Value == 1)
            {
                try
                {
                    if (!IsSorted)
                    {
                        DoSort();
                    }

                    if (deferredChildren != null)
                    {
                        // Set the parents so the children know how to sort themselves
                        foreach (var item in deferredChildren)
                        {
                            item.Parent = parent;
                        }

                        deferredChildren = deferredChildren.OrderBy(x => x).ToList();

                        // Add the deferredChildren
                        foreach (var item in deferredChildren)
                        {
                            this.Add(item);
                        }
                    }
                }
                finally
                {
                    if (deferredChildren != null)
                    {
                        deferredChildren.Clear();
                    }
                    numInits = numInits - 1;
                }

                Initialized?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                numInits = numInits - 1;
            }
        }


        /// <summary>
        /// Repositions this child in the list
        /// </summary>
        public void ReSortChild(TreeNode child)
        {
            if (child == null)
                return;

            List<TreeNode> sorted = this.OrderBy(x => x).ToList();

            // Remove without cleanup
            try
            {
                cleanupBlocker++;
                Remove(child);
            }
            finally
            {
                cleanupBlocker--;
            }

            // Then insert
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] == child)
                {
                    Insert(i, child);
                    return;
                }
            }
        }

        protected override void RemoveItem(int index)
        {
            // Cleanup all the children
            Cleanup(this[index]);

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            // Cleanup all the children
            foreach (var child in this)
            {
                Cleanup(child);
            }

            base.ClearItems();
        }

        private static void Cleanup(TreeNode parent)
        {
            if (cleanupBlocker > 0 ||
                parent.Parent == null)
                return;

            // TODO implement cleanup policy / pattern
            //ICleanupPattern parentAsCleanup = parent as ICleanupPattern;
            //if (parentAsCleanup != null)
            //    parentAsCleanup.DoCleanup();

            //foreach (var child in parent.Children)
            //{
            //    Cleanup(child);
            //}

            parent.Parent = null;
        }

        private void DoSort()
        {
            List<TreeNode> sorted = this.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                int index = IndexOf(sorted[i]);
                if (index != i)
                {
                    Move(IndexOf(sorted[i]), i);
                }
            }
        }
    }
}
