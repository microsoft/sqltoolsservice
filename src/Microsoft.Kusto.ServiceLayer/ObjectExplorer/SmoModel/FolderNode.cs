//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Smo;
using Microsoft.Kusto.ServiceLayer.DataSource;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Represents a folder node in the tree
    /// </summary>
    public class FolderNode : DataSourceTreeNode
    {
        public FolderNode(IDataSource dataSource, DataSourceObjectMetadata objectMetadata) 
            : base(dataSource, objectMetadata)
        {
        }

        /// <summary>
        /// For folders, this copies the context of its parent if available
        /// </summary>
        /// <returns></returns>
        public override object GetContext()
        {
            return Parent?.GetContext();
        }

        /// <summary>
        /// For folders, searches for its parent's SMO object rather than copying for itself
        /// </summary>
        /// <returns><see cref="KustoMetadata"/> from this parent's parent, or null if not found</returns>
        public override DataSourceObjectMetadata GetParentObjectMetadata()
        {
            return ParentAs<DataSourceTreeNode>()?.GetParentObjectMetadata();
        }
    }
}
