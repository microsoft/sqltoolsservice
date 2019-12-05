//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// A Node in the tree representing a SMO-based object
    /// </summary>
    public class DataSourceTreeNode : TreeNode
    {
        public static int FolderSortPriority = 0;
        private static int _nextSortPriority = FolderSortPriority + 1; // 0 is reserved for folders

        protected QueryContext context;

        public DataSourceTreeNode(IDataSource dataSource, DataSourceObjectMetadata objectMetadata) 
            : base(dataSource, objectMetadata)
        {
        }

        protected virtual void OnInitialize()
        {
            // TODO setup initialization
        }
        
        /// <summary>
        /// Is this a system (MSShipped) object?
        /// </summary>
        public bool IsMsShippedOwned { get; set; }

        /// <summary>
        /// Indicates which platforms a node is valid for
        /// </summary>
        public ValidForFlag ValidFor { get; set; }

        /// <summary>
        /// Gets an incrementing sort priority value to assist in automatically sorting
        /// elements in a tree
        /// </summary>
        public static int NextSortPriority
        {
            get
            {
                return System.Threading.Interlocked.Increment(ref _nextSortPriority);
            }
        }

        public virtual void CacheInfoFromModel(DataSourceObjectMetadata objectMetadata)
        {
            base.ObjectMetadata = objectMetadata;
            NodeValue = objectMetadata.Name;
        }
        
        public virtual DataSourceObjectMetadata GetParentObjectMetadata()
        {
            if (ObjectMetadata != null)
            {
                return ObjectMetadata;
            }
            // Return the parent's object, or null if it's not set / not a OETreeNode
            return ParentAs<DataSourceTreeNode>()?.GetParentObjectMetadata();
        }

        public override object GetContext()
        {
            EnsureContextInitialized();
            return context;
        }

        protected virtual void EnsureContextInitialized()
        {
            if (context == null)
            {
                DataSourceObjectMetadata oeParent = GetParentObjectMetadata();
                QueryContext parentContext = Parent?.GetContextAs<QueryContext>();
                if (oeParent != null && parentContext != null)
                {
                    context = parentContext.CopyWithParent(oeParent);
                }
            }
        }
    }
}
