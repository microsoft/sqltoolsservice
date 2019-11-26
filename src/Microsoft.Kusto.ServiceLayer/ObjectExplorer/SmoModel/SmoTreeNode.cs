//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.OEModel
{
    /// <summary>
    /// A Node in the tree representing a SMO-based object
    /// </summary>
    public class OETreeNode : TreeNode
    {
        public static int FolderSortPriority = 0;
        private static int _nextSortPriority = FolderSortPriority + 1; // 0 is reserved for folders

        protected OEQueryContext context;

        public OETreeNode() : base()
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

        public Metadata.Contracts.ObjectMetadata OEObjectMetadata { get; private set; }

        public virtual void CacheInfoFromModel(ObjectMetadata oeObject)
        {
            ObjectMetadata = oeObject;
            NodeValue = oeObject.Name;
        }
        
        public virtual KustoMetadata GetParentSmoObject()
        {
            if (OEObjectMetadata != null)
            {
                return OEObjectMetadata;
            }
            // Return the parent's object, or null if it's not set / not a OETreeNode
            return ParentAs<OETreeNode>()?.GetParentSmoObject();
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
                OEObjectBase oeParent = GetParentSmoObject();
                OEQueryContext parentContext = Parent?.GetContextAs<OEQueryContext>();
                if (oeParent != null && parentContext != null)
                {
                    context = parentContext.CopyWithParent(oeParent);
                }
            }
        }
    }
}
