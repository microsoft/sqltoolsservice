//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Context object containing key properties needed to query for SMO objects
    /// </summary>
    public class SmoQueryContext
    {
        /// <summary>
        /// Creates a context object with a server to use as the basis for any queries
        /// </summary>
        /// <param name="server"></param>
        public SmoQueryContext(Server server, IMultiServiceProvider serviceProvider)
        {
            Server = server;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// The server SMO will query against
        /// </summary>
        public Server Server { get; private set; }

        /// <summary>
        /// Optional Database context object to query against
        /// </summary>
        public Database Database { get; set; }

        /// <summary>
        /// Parent of a give node to use for queries
        /// </summary>
        public SmoObjectBase Parent { get; set; }

        /// <summary>
        /// A query loader that can be used to find <see cref="SmoQuerier"/> objects
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
            return Parent as T;
        }

        /// <summary>
        /// Gets the <see cref="ObjectExplorerService"/> if available, by looking it up
        /// from the <see cref="ServiceProvider"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ServiceProvider"/> is not set or the <see cref="ObjectExplorerService"/>
        /// isn't available from that provider
        /// </exception>
        public ObjectExplorerService GetObjectExplorerService()
        {
            if (ServiceProvider == null)
            {
                throw new InvalidOperationException(SR.ServiceProviderNotSet);
            }
            ObjectExplorerService service = ServiceProvider.GetService<ObjectExplorerService>();
            if (service == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SR.ServiceNotFound, nameof(ObjectExplorerService)));
            }

            return service;
        }
        /// <summary>
        /// Copies the context for use by another node
        /// </summary>
        /// <param name="parent">New Parent to set</param>
        /// <returns>new <see cref="SmoQueryContext"/> with all fields except <see cref="Parent"/> the same</returns>
        public SmoQueryContext CopyWithParent(SmoObjectBase parent)
        {
            SmoQueryContext context = new SmoQueryContext(this.Server, this.ServiceProvider)
            {
                Database = this.Database,
                Parent = parent
            };
            return context;
        }
    }
}
