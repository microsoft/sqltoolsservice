//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// A Node in the tree representing a SMO-based object
    /// </summary>
    public class SmoTreeNode : TreeNode
    {
        public static int FolderSortPriority = 0;
        private static int _nextSortPriority = FolderSortPriority + 1; // 0 is reserved for folders

        protected SmoQueryContext context;

        public SmoTreeNode() : base()
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

        public NamedSmoObject SmoObject { get; private set; }

        public virtual void CacheInfoFromModel(NamedSmoObject smoObject)
        {
            SmoObject = smoObject;
            NodeValue = smoObject.Name;
            ScriptSchemaObjectBase schemaBasecObject = smoObject as ScriptSchemaObjectBase;
            ObjectMetadata = new Metadata.Contracts.ObjectMetadata();
            ObjectMetadata.Name = smoObject.Name;

            try
            {
                if(smoObject.Urn != null)
                {
                    ObjectMetadata.MetadataTypeName = smoObject.Urn.Type;
                }
            }
            catch
            {
                //Ignore the exception, sometimes the urn returns exception and I' not sure why
            }
            
            if (schemaBasecObject != null)
            {
                ObjectMetadata.Schema = schemaBasecObject.Schema;
                if (!string.IsNullOrEmpty(ObjectMetadata.Schema))
                {
                    NodeValue = $"{ObjectMetadata.Schema}.{smoObject.Name}";
                }
            }
        }
        
        public virtual NamedSmoObject GetParentSmoObject()
        {
            if (SmoObject != null)
            {
                return SmoObject;
            }
            // Return the parent's object, or null if it's not set / not a SmoTreeNode
            return ParentAs<SmoTreeNode>()?.GetParentSmoObject();
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
                SmoObjectBase smoParent = GetParentSmoObject();
                SmoQueryContext parentContext = Parent?.GetContextAs<SmoQueryContext>();
                if (smoParent != null && parentContext != null)
                {
                    context = parentContext.CopyWithParent(smoParent);
                }
            }
        }
    }
}
