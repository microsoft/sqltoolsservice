//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    public class SmoChildFactoryBase : ChildFactory
    {
        public override IEnumerable<string> ApplicableParents()
        {
            return null;
        }
        
        public override IEnumerable<TreeNode> Expand(TreeNode parent)
        {
            try
            {
                List<TreeNode> allChildren = new List<TreeNode>();
                OnExpandPopulateFolders(allChildren, parent);
                RemoveFoldersFromInvalidSqlServerVersions(allChildren, parent);
                OnExpandPopulateNonFolders(allChildren, parent);
                OnBeginAsyncOperations(parent);
                return allChildren;
            }
            finally
            {
            }
        }
        
        /// <summary>
        /// Populates any folders for a given parent node 
        /// </summary>
        /// <param name="allChildren">List to which nodes should be added</param>
        /// <param name="parent">Parent the nodes are being added to</param>
        protected virtual void OnExpandPopulateFolders(IList<TreeNode> allChildren, TreeNode parent)
        {
        }

        /// <summary>
        /// Populates any non-folder nodes such as specific items in the tree.
        /// </summary>
        /// <param name="allChildren">List to which nodes should be added</param>
        /// <param name="parent">Parent the nodes are being added to</param>
        protected virtual void OnExpandPopulateNonFolders(IList<TreeNode> allChildren, TreeNode parent)
        {
            if (ChildQuerierTypes == null)
            {
                // This node does not support non-folder children
                return;
            }
            SmoQueryContext context = parent.GetContextAs<SmoQueryContext>();
            Validate.IsNotNull(nameof(context), context);
            IEnumerable<SmoQuerier> queriers = context.ServiceProvider.GetServices<SmoQuerier>(q => IsCompatibleQuerier(q));
            var filters = this.Filters;
            var validForFlag = ServerVersionHelper.GetValidForFlag(context.SqlServerType);

            foreach (var querier in queriers)
            {
                string propertyFilter = GetProperyFilter(filters, querier.GetType(), validForFlag);

                foreach (var smoObject in querier.Query(context, propertyFilter))
                {
                    if (smoObject == null)
                    {
                        Console.WriteLine("smoObject should not be null");
                    }
                    TreeNode childNode = CreateChild(parent, smoObject);
                    if (childNode != null && PassesFinalFilters(childNode, smoObject) && !ShouldFilterNode(childNode, validForFlag))
                    {
                        allChildren.Add(childNode);
                    }
                }
            }
        }

        private bool ShouldFilterNode(TreeNode childNode, ValidForFlag validForFlag)
        {
            bool filterTheNode = false;
            SmoTreeNode smoTreeNode = childNode as SmoTreeNode;
            if (smoTreeNode != null && smoTreeNode.ValidFor != 0)
            {
                if (!(smoTreeNode.ValidFor.HasFlag(validForFlag)))
                {
                    filterTheNode = true;
                }
            }

            return filterTheNode;
        }

        private string GetProperyFilter(IEnumerable<NodeFilter> filters, Type querierType, ValidForFlag validForFlag)
        {
            string filter = string.Empty;
            if (filters != null)
            {
                var filtersToApply = filters.Where(f => f.CanApplyFilter(querierType, validForFlag)).ToList();
                filter = string.Empty;
                if (filtersToApply.Any())
                {
                    filter = NodeFilter.ConcatProperties(filtersToApply);
                }
            }

            return filter;
        }

        private bool IsCompatibleQuerier(SmoQuerier querier)
        {
            if (ChildQuerierTypes == null)
            {
                return false;
            }

            Type actualType = querier.GetType();
            foreach (Type childType in ChildQuerierTypes)
            {
                // We will accept any querier that is compatible with the listed querier type
                if (childType.IsAssignableFrom(actualType))
                {
                    return true;
                }
            }
            return false;

        }
        
        /// <summary>
        /// Filters out invalid folders if they cannot be displayed for the current server version
        /// </summary>
        /// <param name="allChildren">List to which nodes should be added</param>
        /// <param name="parent">Parent the nodes are being added to</param>
        protected virtual void RemoveFoldersFromInvalidSqlServerVersions(IList<TreeNode> allChildren, TreeNode parent)
        {
        }

        // TODO Assess whether async operations node is required
        protected virtual void OnBeginAsyncOperations(TreeNode parent)
        {
        }

        public override bool CanCreateChild(TreeNode parent, object context)
        {
            return false;
        }

        public override TreeNode CreateChild(TreeNode parent, object context)
        {
            throw new NotImplementedException();
        }

        protected virtual void InitializeChild(TreeNode child, object context)
        {
            NamedSmoObject smoObj = context as NamedSmoObject;
            if (smoObj == null)
            {
                Debug.WriteLine("context is not a NamedSmoObject. type: " + context.GetType());
            }
            else
            {
                SmoTreeNode childAsMeItem = (SmoTreeNode)child;
                childAsMeItem.CacheInfoFromModel(smoObj);
            }
        }

        internal virtual Type[] ChildQuerierTypes { 
            get
            {
                return null;
            }
        }

        public override IEnumerable<NodeFilter> Filters
        {
            get
            {
                return Enumerable.Empty<NodeFilter>();
            }
        }

        /// <summary>
        /// Returns true if any final validation of the object to be added passes, and false
        /// if validation fails. This provides a chance to filter specific items out of a list 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="contextObject"></param>
        /// <returns>boolean</returns>
        public virtual bool PassesFinalFilters(TreeNode parent, object context)
        {
            return true;
        }
    }
}
