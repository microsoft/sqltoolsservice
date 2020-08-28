//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;
using Microsoft.Kusto.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Metadata;

namespace Microsoft.Kusto.ServiceLayer.ObjectExplorer.DataSourceModel
{
    /// <summary>
    /// Context object containing key properties needed to query for SMO objects
    /// </summary>
    public class QueryContext
    {
        public IDataSource DataSource { get; private set; }

        /// <summary>
        /// Creates a context object with a server to use as the basis for any queries
        /// </summary>
        /// <param name="server"></param>
        public QueryContext(IDataSource dataSource, IMultiServiceProvider serviceProvider)
        {
            DataSource = dataSource;
            ServiceProvider = serviceProvider;
        }
        
        /// <summary>
        /// Parent of a give node to use for queries
        /// </summary>
        public DataSourceObjectMetadata ParentObjectMetadata { get; set; }

        /// <summary>
        /// A query loader that can be used to find <see cref="DataSourceQuerier"/> objects
        /// for specific SMO types
        /// </summary>
        public IMultiServiceProvider ServiceProvider { get; private set; }
        
        /// <summary>
        /// Helper method to cast a parent to a specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ParentAs<T>()
            where T : TreeNode
        {
            return ParentObjectMetadata as T;
        }

        /// <summary>
        /// Copies the context for use by another node
        /// </summary>
        /// <param name="parent">New Parent to set</param>
        /// <returns>new <see cref="QueryContext"/> with all fields except <see cref="ParentObjectMetadata"/> the same</returns>
        public QueryContext CopyWithParent(DataSourceObjectMetadata parent)
        {
            QueryContext context = new QueryContext(this.DataSource, this.ServiceProvider)
            {
                ParentObjectMetadata = parent
            };
            return context;
        }
    }
}
