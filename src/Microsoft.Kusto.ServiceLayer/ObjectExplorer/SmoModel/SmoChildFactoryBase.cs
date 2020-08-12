//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    public class DataSourceChildFactoryBase : ChildFactory
    {
        private IEnumerable<NodeSmoProperty> smoProperties;
        public override IEnumerable<string> ApplicableParents()
        {
            return null;
        }

        public override IEnumerable<TreeNode> Expand(TreeNode parent, bool refresh, string name, bool includeSystemObjects, CancellationToken cancellationToken)
        {
            throw new NotImplementedException(); // Moved to TreeNode.cs
        }

        private bool ShouldFilterNode(TreeNode childNode, ValidForFlag validForFlag)
        {
            bool filterTheNode = false;
            
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

        private bool IsCompatibleQuerier(DataSourceQuerier querier)
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

        public override bool CanCreateChild(TreeNode parent, object context)
        {
            return false;
        }

        public override TreeNode CreateChild(TreeNode parent, DataSourceObjectMetadata childMetadata)
        {
            throw new NotImplementedException();
        }

        protected virtual void InitializeChild(TreeNode parent, TreeNode child, object context)
        {
            DataSourceObjectMetadata objectMetadata = context as DataSourceObjectMetadata;
            if (objectMetadata == null)
            {
                Debug.WriteLine("context is not a DataSourceObjectMetadata type: " + context.GetType());
            }
            else
            {
                smoProperties = SmoProperties;
                DataSourceTreeNode childAsMeItem = (DataSourceTreeNode)child;
                childAsMeItem.CacheInfoFromModel(objectMetadata);
                QueryContext oeContext = parent.GetContextAs<QueryContext>();

                // If node has custom name, replaced it with the name already set
                string customizedName = GetNodeCustomName(context, oeContext);
                if (!string.IsNullOrEmpty(customizedName))
                {
                    childAsMeItem.NodeValue = customizedName;
                    childAsMeItem.NodePathName = GetNodePathName(context);
                }

                childAsMeItem.NodeSubType = GetNodeSubType(context, oeContext);
                childAsMeItem.NodeStatus = GetNodeStatus(context, oeContext);
            }
        }

        internal virtual Type[] ChildQuerierTypes
        {
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

        public override IEnumerable<NodeSmoProperty> SmoProperties
        {
            get
            {
                return Enumerable.Empty<NodeSmoProperty>();
            }
        }

        internal IEnumerable<NodeSmoProperty> CachedSmoProperties
        {
            get
            {
                return smoProperties == null ? SmoProperties : smoProperties;
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

        public override string GetNodeSubType(object objectMetadata, QueryContext oeContext)
        {
            return string.Empty;
        }

        public override string GetNodeStatus(object objectMetadata, QueryContext oeContext)
        {
            return string.Empty;
        }

        public static bool IsPropertySupported(string propertyName, QueryContext context, DataSourceObjectMetadata objectMetadata, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            return true;
        }

        public override string GetNodeCustomName(object objectMetadata, QueryContext oeContext)
        {
            return (objectMetadata as DataSourceObjectMetadata).PrettyName;
        }

        public override string GetNodePathName(object objectMetadata)
        {
            return (objectMetadata as DataSourceObjectMetadata).Urn;
        }
    }
}
