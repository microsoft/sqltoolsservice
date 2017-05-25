//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.ObjectExplorer.Nodes;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectExplorer.SmoModel
{
    /// <summary>
    /// Context object containing key properties needed to query for SMO objects
    /// </summary>
    public class SmoQueryContext
    {
        private Server server;
        private Database database;
        private SmoObjectBase parent;
        private SmoWrapper smoWrapper;

        /// <summary>
        /// Creates a context object with a server to use as the basis for any queries
        /// </summary>
        /// <param name="server"></param>
        public SmoQueryContext(Server server, IMultiServiceProvider serviceProvider)
            : this(server, serviceProvider, null)
        {
        }

        internal SmoQueryContext(Server server, IMultiServiceProvider serviceProvider, SmoWrapper serverManager)
        {
            this.server = server;
            ServiceProvider = serviceProvider;
            this.smoWrapper = serverManager ?? new SmoWrapper();
        }

        /// <summary>
        /// The server type 
        /// </summary>
        public SqlServerType SqlServerType { get; set; }

        /// <summary>
        /// The server SMO will query against
        /// </summary>
        public Server Server { 
            get
            {
                return GetObjectWithOpenedConnection(server);
            } 
        }

        /// <summary>
        /// Optional Database context object to query against
        /// </summary>
        public Database Database { 
            get
            {
                return GetObjectWithOpenedConnection(database);
            }
            set
            {
                database = value;
            }
        }

        /// <summary>
        /// Parent of a give node to use for queries
        /// </summary>
        public SmoObjectBase Parent { 
            get
            {
                return GetObjectWithOpenedConnection(parent);
            }
            set
            {
                parent = value;
            }
        }

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
                throw new InvalidOperationException(SqlTools.Hosting.SR.ServiceProviderNotSet);
            }
            ObjectExplorerService service = ServiceProvider.GetService<ObjectExplorerService>();
            if (service == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, 
                    SqlTools.Hosting.SR.ServiceNotFound, nameof(ObjectExplorerService)));
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
            SmoQueryContext context = new SmoQueryContext(this.Server, this.ServiceProvider, this.smoWrapper)
            {
                database = this.Database,
                Parent = parent,
                SqlServerType = this.SqlServerType
            };
            return context;
        }

        private T GetObjectWithOpenedConnection<T>(T smoObj)
            where T : SmoObjectBase
        {
            if (smoObj != null)
            {
                EnsureConnectionOpen(smoObj);
            }
            return smoObj;
        }

        /// <summary>
        /// Ensures the server objects connection context is open. This is used by all child objects, and 
        /// the only way to easily access is via the server object. This should be called during access of
        /// any of the object properties
        /// </summary>
        private void EnsureConnectionOpen(SmoObjectBase smoObj)
        {
            if (!smoWrapper.IsConnectionOpen(smoObj))
            {
                // We have a closed server connection. Reopen this
                // Note: not currently catching connection exceptions. Expect this to bubble
                // up to calling methods and be logged there as this would be happening there in any case
                smoWrapper.OpenConnection(smoObj);
            }
        }
    }
}
