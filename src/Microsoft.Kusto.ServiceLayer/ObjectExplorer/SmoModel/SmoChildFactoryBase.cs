//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    public class SmoChildFactoryBase : ChildFactory
    {
        private IEnumerable<NodeSmoProperty> smoProperties;
        public override IEnumerable<string> ApplicableParents()
        {
            return null;
        }

        public override IEnumerable<TreeNode> Expand(TreeNode parent, bool refresh, string name, bool includeSystemObjects, CancellationToken cancellationToken)
        {
            List<TreeNode> allChildren = new List<TreeNode>();

            try
            {
                OnExpandPopulateFoldersAndFilter(allChildren, parent, includeSystemObjects);
                RemoveFoldersFromInvalidSqlServerVersions(allChildren, parent);
                OnExpandPopulateNonFolders(allChildren, parent, refresh, name, cancellationToken);
                OnBeginAsyncOperations(parent);
            }
            catch(Exception ex)
            {
                string error = string.Format(CultureInfo.InvariantCulture, "Failed expanding oe children. parent:{0} error:{1} inner:{2} stacktrace:{3}", 
                    parent != null ? parent.GetNodePath() : "", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                Logger.Write(TraceEventType.Error, error);
                throw ex;
            }
            finally
            {
            }
            return allChildren;
        }

        private void OnExpandPopulateFoldersAndFilter(List<TreeNode> allChildren, TreeNode parent, bool includeSystemObjects)
        {
            SmoQueryContext context = parent.GetContextAs<SmoQueryContext>();
            OnExpandPopulateFolders(allChildren, parent);
            if (!includeSystemObjects)
            {
                allChildren.RemoveAll(x => x.IsSystemObject);
            }
            if (context != null && context.ValidFor != 0 && context.ValidFor != ValidForFlag.All)
            {
                allChildren.RemoveAll(x =>
                {
                    FolderNode folderNode = x as FolderNode;
                    if (folderNode != null && !ServerVersionHelper.IsValidFor(context.ValidFor, folderNode.ValidFor))
                    {
                        return true;
                    }
                    return false;
                });
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
        protected virtual void OnExpandPopulateNonFolders(IList<TreeNode> allChildren, TreeNode parent, bool refresh, string name, CancellationToken cancellationToken)
        {
            Logger.Write(TraceEventType.Verbose, string.Format(CultureInfo.InvariantCulture, "child factory parent :{0}", parent.GetNodePath()));

            if (ChildQuerierTypes == null)
            {
                // This node does not support non-folder children
                return;
            }
            SmoQueryContext context = parent.GetContextAs<SmoQueryContext>();
            Validate.IsNotNull(nameof(context), context);

            var serverValidFor = context.ValidFor;
            if (ShouldFilterNode(parent, serverValidFor))
            {
                return;
            }

            IEnumerable<SmoQuerier> queriers = context.ServiceProvider.GetServices<SmoQuerier>(q => IsCompatibleQuerier(q));
            var filters = this.Filters.ToList();
            var smoProperties = this.SmoProperties.Where(p => ServerVersionHelper.IsValidFor(serverValidFor, p.ValidFor)).Select(x => x.Name);
            if (!string.IsNullOrEmpty(name))
            {
                filters.Add(new NodeFilter
                {
                    Property = "Name",
                    Type = typeof(string),
                    Values = new List<object> { name },
                });
            }
            foreach (var querier in queriers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!querier.IsValidFor(serverValidFor))
                {
                    continue;
                }
                string propertyFilter = GetProperyFilter(filters, querier.GetType(), serverValidFor);
                try
                {
                    var smoObjectList = querier.Query(context, propertyFilter, refresh, smoProperties).ToList();
                    foreach (var smoObject in smoObjectList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (smoObject == null)
                        {
                            Logger.Write(TraceEventType.Error, "smoObject should not be null");
                        }
                        TreeNode childNode = CreateChild(parent, smoObject);
                        if (childNode != null && PassesFinalFilters(childNode, smoObject) && !ShouldFilterNode(childNode, serverValidFor))
                        {
                            allChildren.Add(childNode);
                        }
                    }

                }
                catch (Exception ex)
                {
                    string error = string.Format(CultureInfo.InvariantCulture, "Failed getting smo objects. parent:{0} querier: {1} error:{2} inner:{3} stacktrace:{4}",
                    parent != null ? parent.GetNodePath() : "", querier.GetType(), ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                    Logger.Write(TraceEventType.Error, error);
                    throw ex;
                }
            }
        }

        private bool ShouldFilterNode(TreeNode childNode, ValidForFlag validForFlag)
        {
            bool filterTheNode = false;
            SmoTreeNode smoTreeNode = childNode as SmoTreeNode;
            if (smoTreeNode != null)
            {
                if (!ServerVersionHelper.IsValidFor(validForFlag, smoTreeNode.ValidFor))
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

        protected virtual void InitializeChild(TreeNode parent, TreeNode child, object context)
        {
            NamedSmoObject smoObj = context as NamedSmoObject;
            if (smoObj == null)
            {
                Debug.WriteLine("context is not a NamedSmoObject. type: " + context.GetType());
            }
            else
            {
                smoProperties = SmoProperties;
                SmoTreeNode childAsMeItem = (SmoTreeNode)child;
                childAsMeItem.CacheInfoFromModel(smoObj);
                SmoQueryContext smoContext = parent.GetContextAs<SmoQueryContext>();

                // If node has custom name, replaced it with the name already set
                string customizedName = GetNodeCustomName(context, smoContext);
                if (!string.IsNullOrEmpty(customizedName))
                {
                    childAsMeItem.NodeValue = customizedName;
                    childAsMeItem.NodePathName = GetNodePathName(context);
                }

                childAsMeItem.NodeSubType = GetNodeSubType(context, smoContext);
                childAsMeItem.NodeStatus = GetNodeStatus(context, smoContext);
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

        public override string GetNodeSubType(object smoObject, SmoQueryContext smoContext)
        {
            return string.Empty;
        }

        public override string GetNodeStatus(object smoObject, SmoQueryContext smoContext)
        {
            return string.Empty;
        }

        public static bool IsPropertySupported(string propertyName, SmoQueryContext context, NamedSmoObject smoObj, IEnumerable<NodeSmoProperty> supportedProperties)
        {
            var property = supportedProperties.FirstOrDefault(x => string.Compare(x.Name, propertyName, StringComparison.InvariantCultureIgnoreCase) == 0);
            if (property != null)
            {
                return ServerVersionHelper.IsValidFor(context.ValidFor, property.ValidFor);
            }
            else
            {
                // Return true if cannot find the proeprty, SMO still tries to get that property but adding the property to supported list can make loading the nodes faster
                Logger.Write(TraceEventType.Verbose, $"Smo property name {propertyName} for Smo type {smoObj.GetType()} is not added as supported properties. This can cause the performance of loading the OE nodes");
                return true;
            }
        }

        public override string GetNodeCustomName(object smoObject, SmoQueryContext smoContext)
        {
            return string.Empty;
        }

        public override string GetNodePathName(object smoObject)
        {
            return (smoObject as NamedSmoObject).Name;
        }
    }
}
